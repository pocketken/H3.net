#!/usr/bin/env python3
"""Emit the benchmark.md markdown tables from benchmark-data.json.

Companion to generate_charts.py: same input, but writes GitHub-flavored markdown
tables (polygon fill sweep, and the per-operation speed/allocation matrix) instead
of charts.  Nothing is hand-typed; every number comes from the data file, which is
produced by build_data.py from the BenchmarkDotNet report CSVs.

    generate_tables.py <benchmark-data.json> [out.md]

With no out.md, writes to stdout.
"""
import json
import sys


def _t(us):
    """Format a microsecond value as us / ms / s."""
    if us is None:
        return ""
    if us < 1000:
        return f"{us:.1f} µs"
    if us < 1_000_000:
        return f"{us / 1000:.2f} ms"
    return f"{us / 1_000_000:.2f} s"


def _ns(ns):
    if ns is None:
        return ""
    if ns < 1000:
        return f"{ns:.1f} ns"
    if ns < 1_000_000:
        return f"{ns / 1000:.1f} µs"
    return f"{ns / 1_000_000:.2f} ms"


def _b(v):
    if v is None:
        return ""
    if v == 0:
        return "0 B"
    if v < 1024:
        return f"{v} B"
    if v < 1024 * 1024:
        return f"{v / 1024:.1f} KB"
    return f"{v / (1024 * 1024):.1f} MB"


def sweep_table(data, resolutions=(4, 6, 8, 10, 11, 12)):
    rows = {r["resolution"]: r for r in data["polygon_sweep"]["rows"]}
    out = ["### polygonToCells: fill time vs output size", "",
           "| Resolution | Output cells | H3.NET.Native | pocketken.H3 | ParallelFill |",
           "| ---: | ---: | ---: | ---: | ---: |"]
    for res in resolutions:
        r = rows[res]
        out.append(f"| {res} | {r['cells']:,} | {_t(r['native_us'])} | "
                   f"{_t(r['pocketken_us'])} | {_t(r.get('pocketken_parallel_us'))} |")
    return "\n".join(out)


def matrix_table(data):
    rows = sorted(data["op_matrix"]["rows"],
                  key=lambda r: r["span_ratio"] if r["span_ratio"] is not None else r["streaming_ratio"])
    out = ["### pocketken.H3 vs the native binding, per operation", "",
           "Speed is time; ratios are relative to the native binding (streaming vs its array call, "
           "span vs its Into/span call). Allocation is bytes per op.", "",
           "| Operation | pocketken streaming | pocketken span | H3.NET.Native | streaming | span | pk alloc (stream / span) | native alloc |",
           "| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |"]
    for r in rows:

        def rx(v):
            return f"{v:.2f}x" if v is not None else ""

        out.append(
            f"| {r['op']} | {_ns(r['pocketken_streaming_ns'])} | {_ns(r['pocketken_span_ns'])} | "
            f"{_ns(r['native_ns'])} | {rx(r['streaming_ratio'])} | {rx(r['span_ratio'])} | "
            f"{_b(r['pocketken_streaming_alloc_b'])} / {_b(r['pocketken_span_alloc_b'])} | "
            f"{_b(r['native_alloc_b'])} |")
    return "\n".join(out)


def main():
    data = json.load(open(sys.argv[1], encoding="utf-8"))
    md = "\n\n".join([
        matrix_table(data),
        sweep_table(data),
    ]) + "\n"
    if len(sys.argv) > 2:
        with open(sys.argv[2], "w", encoding="utf-8") as fh:
            fh.write(md)
        print(f"wrote {sys.argv[2]}")
    else:
        print(md)


if __name__ == "__main__":
    main()
