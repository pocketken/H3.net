#!/usr/bin/env python3
"""Assemble benchmark-data.json straight from the BenchmarkDotNet report artifacts.

No number is hand-typed: means, ratios and allocations are parsed from the
per-class *-report.csv files under a results dir, and the provenance block is
parsed from a *-report-github.md header.  Run:

    build_data.py <results-dir> <out.json>

e.g. build_data.py perf/nativecompare/net10/results perf/charts/benchmark-data.json
"""
import csv
import glob
import json
import os
import re
import sys

# --- unit parsing ---------------------------------------------------------------
_TIME = {"ns": 1e-3, "us": 1.0, "µs": 1.0, "μs": 1.0, "ms": 1e3, "s": 1e6}  # -> microseconds
_TIME_NS = {"ns": 1.0, "us": 1e3, "µs": 1e3, "μs": 1e3, "ms": 1e6, "s": 1e9}  # -> nanoseconds
_BYTES = {"B": 1, "KB": 1024, "MB": 1024 * 1024, "GB": 1024 * 1024 * 1024}


def _num(cell):
    return float(cell.replace(",", "").strip().split()[0])


def _to_us(cell):
    v, u = cell.replace(",", "").strip().split()
    return _num(v) * _TIME[u]


def _to_ns(cell):
    v, u = cell.replace(",", "").strip().split()
    return _num(v) * _TIME_NS[u]


def _to_bytes(cell):
    cell = cell.replace(",", "").strip()
    if cell in ("", "-", "NA"):
        return None
    v, u = cell.split()
    return int(round(float(v) * _BYTES[u]))


def _rows(results_dir, cls):
    path = glob.glob(os.path.join(results_dir, f"*{cls}-report.csv"))[0]
    with open(path, encoding="utf-8") as fh:
        return list(csv.DictReader(fh))


def _find(rows, method, **eq):
    for r in rows:
        if r["Method"].strip("'\" ") == method and all(r.get(k, "").strip() == v for k, v in eq.items()):
            return r
    raise KeyError(f"{method} {eq}")


# chart footer stamp: CPU, runtime and tool, parsed from the BDN report header
def provenance(results_dir):
    head = open(glob.glob(os.path.join(results_dir, "*-report-github.md"))[0], encoding="utf-8").read()
    bdn = re.search(r"BenchmarkDotNet v[\d.]+", head)
    cpu = re.search(r"\n([A-Za-z].*(?:Processor|CPU|Ryzen|EPYC|Xeon).*cores)", head)
    net = re.search(r"\[Host\][^\n]*:\s*(\.NET [\d.]+)", head)
    return {
        "cpu": cpu.group(1).strip() if cpu else "unknown",
        "dotnet": net.group(1) if net else "unknown",
        "tool": bdn.group(0) if bdn else "BenchmarkDotNet",
    }


def main():
    results_dir, out = sys.argv[1], sys.argv[2]

    h3 = _rows(results_dir, "H3Benchmarks")
    sweep = _rows(results_dir, "H3PolygonSweepBenchmarks")
    par = _rows(results_dir, "PolygonParallelBenchmarks")
    hier = _rows(results_dir, "HierarchyBenchmarks")
    span = _rows(results_dir, "SpanApiBenchmarks")

    # --- across-the-board matrix: pocketken vs the native binding, per op ------------
    # Where pocketken exposes both a streaming (allocating) and a span (zero-alloc,
    # into a caller buffer) API, show BOTH, each against its matching native variant
    # (streaming vs the binding's array call, span vs its Into/span call) so every
    # comparison is apples-to-apples.  A None column means that variant does not exist.
    # (op, rows, pk_streaming, pk_span, native_array, native_span)
    matrix_spec = [
        ("latLngToCell",   h3,   "pocketken.H3 FromLatLng",               None,                                  "H3.NET.Native FromLatLng",              None),
        ("cellToParent",   hier, "pocketken.H3 GetParentForResolution",   None,                                  "H3.NET.Native CellToParent",            None),
        ("cellToChildren", hier, "pocketken.H3 GetChildrenForResolution", "pocketken.H3 GetChildrenForResolution (span)", "H3.NET.Native CellToChildren (array)", "H3.NET.Native CellToChildrenInto (span)"),
        ("gridDiskDist",   span, "streaming GridDiskDistances",           "span GridDiskDistances",              "H3.NET.Native GridDiskDistances (array)", "H3.NET.Native GridDiskDistancesInto (span)"),
        ("gridRing",       span, "streaming GridRingUnsafe",              "span GridRingUnsafe",                 "H3.NET.Native GridRing (array)",        "H3.NET.Native GridRingInto (span)"),
        ("gridPathCells",  span, "streaming GridPathCells",               "span GridPathCells",                  "H3.NET.Native GridPathCells (array)",   "H3.NET.Native GridPathCellsInto (span)"),
        ("cellToBoundary", span, "streaming GetCellBoundaryVertices",     "span GetCellBoundaryVertices",        "H3.NET.Native GetBoundary",             None),
        ("compactCells",   span, "streaming CompactCells",                "span CompactCells",                   "H3.NET.Native CompactCells (array)",    "H3.NET.Native CompactCellsInto (span)"),
        ("uncompactCells", span, "streaming UncompactCells",              "span UncompactCells",                 "H3.NET.Native UncompactCells (array)",  "H3.NET.Native UncompactCellsInto (span)"),
        ("polygonToCells", h3,   "pocketken.H3 Polyfill.Fill",            None,                                  "H3.NET.Native ToCells",                 None),
    ]

    def _cell(rows, method):
        if not method:
            return None, None
        r = _find(rows, method)
        return round(_to_ns(r["Mean"]), 1), _to_bytes(r["Allocated"])

    matrix = []
    for op, rows, pk_s, pk_sp, nat_a, nat_sp in matrix_spec:
        ps, ps_a = _cell(rows, pk_s)
        psp, psp_a = _cell(rows, pk_sp)
        na, na_a = _cell(rows, nat_a)
        nasp, nasp_a = _cell(rows, nat_sp)
        matrix.append({
            "op": op,
            "pocketken_streaming_ns": ps, "pocketken_span_ns": psp,
            "native_ns": na, "native_span_ns": nasp,
            # streaming vs the binding's array call; span vs its Into/span call
            # where it has one, else vs the array call
            "streaming_ratio": round(ps / na, 3) if ps and na else None,
            "span_ratio": round(psp / (nasp or na), 3) if psp else None,
            "pocketken_streaming_alloc_b": ps_a, "pocketken_span_alloc_b": psp_a,
            "native_alloc_b": na_a, "native_span_alloc_b": nasp_a,
        })

    # --- polygon sweep: native ToCells vs pocketken Fill, per resolution ---------
    native_cells = {4: 1, 5: 9, 6: 65, 7: 455, 8: 3189, 9: 22334,
                    10: 156334, 11: 1094337, 12: 7660354}
    sweep_rows = []
    for res in sorted(native_cells):
        rs = str(res)
        nat = _find(sweep, "H3.NET.Native ToCells", Resolution=rs)
        pk = _find(sweep, "pocketken.H3 Polyfill.Fill", Resolution=rs)
        row = {
            "resolution": res,
            "cells": native_cells[res],
            "native_us": round(_to_us(nat["Mean"]), 3),
            "pocketken_us": round(_to_us(pk["Mean"]), 3),
            "native_alloc_b": _to_bytes(nat["Allocated"]),
            "pocketken_alloc_b": _to_bytes(pk["Allocated"]),
        }
        try:
            pf = _find(par, "pocketken.H3 ParallelFill", Resolution=rs)
            row["pocketken_parallel_us"] = round(_to_us(pf["Mean"]), 1)
        except KeyError:
            pass
        sweep_rows.append(row)

    poly22k = next(r for r in sweep_rows if r["resolution"] == 9)
    poly77m = next(r for r in sweep_rows if r["resolution"] == 12)

    # --- small workloads: raw / native / pocketken -------------------------------
    def sw(op, impl, method, cat=None):
        eq = {"Categories": cat} if cat else {}
        r = _find(h3, method, **eq)
        d = {"op": op, "impl": impl, "mean_ns": round(_to_ns(r["Mean"]), 1),
             "ratio": float(r["Ratio"]), "alloc_b": _to_bytes(r["Allocated"])}
        return d

    small = [
        sw("LatLngToCell", "raw libh3", "raw libh3 latLngToCell"),
        sw("LatLngToCell", "H3.NET.Native", "H3.NET.Native FromLatLng"),
        sw("LatLngToCell", "pocketken.H3", "pocketken.H3 FromLatLng"),
        sw("GridDisk", "raw libh3", "raw libh3 gridDisk"),
        sw("GridDisk", "H3.NET.Native", "H3.NET.Native GridDisk"),
        # apples-to-apples buffer fill (cells+distances into a caller span)
        sw("GridDisk", "pocketken.H3", "pocketken.H3 GridDiskDistances (span)"),
        sw("PolygonToCells", "H3.NET.Native", "H3.NET.Native ToCells", cat="PolygonToCells"),
        sw("PolygonToCells", "pocketken.H3", "pocketken.H3 Polyfill.Fill", cat="PolygonToCells"),
        {"op": "Polyfill 22k", "impl": "H3.NET.Native", "alloc_b": poly22k["native_alloc_b"]},
        {"op": "Polyfill 22k", "impl": "pocketken.H3", "alloc_b": poly22k["pocketken_alloc_b"]},
        {"op": "Polyfill 7.7M", "impl": "H3.NET.Native", "alloc_b": poly77m["native_alloc_b"]},
        {"op": "Polyfill 7.7M", "impl": "pocketken.H3", "alloc_b": poly77m["pocketken_alloc_b"]},
    ]

    data = {
        "provenance": provenance(results_dir),
        "op_matrix": {
            "note": "idiomatic pocketken (streaming/normal API) vs the binding's idiomatic call, one ratio (pocketken/native) per op; below 1.0 = pocketken faster.",
            "rows": matrix,
        },
        "polygon_sweep": {
            "note": "~0.5deg SF box, res 4-12, same box+resolution per point. native = H3.NET.Native ToCells; pocketken = Polyfill.Fill (sequential); parallel = ParallelFill (res 9-12).",
            "rows": sweep_rows,
        },
        "small_workloads": {
            "note": "LatLngToCell/GridDisk baseline = raw libh3 (P/Invoke). GridDisk pocketken = GridDiskDistances into a caller buffer (apples-to-apples with the raw buffer fill; cells+distances). PolygonToCells = res-9 SF box. Polyfill 22k/7.7M = 0.5deg box res9/res12 (allocation).",
            "rows": small,
        },
    }

    with open(out, "w", encoding="utf-8") as fh:
        json.dump(data, fh, indent=2)
        fh.write("\n")
    print(f"wrote {out} from {results_dir}")


if __name__ == "__main__":
    main()
