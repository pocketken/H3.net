# /// script
# requires-python = ">=3.10"
# dependencies = ["matplotlib"]
# ///
#!/usr/bin/env python3
"""Generate pocketken.H3-vs-native benchmark charts (PNG + SVG) from a results snapshot.

Adapted from H3.NET.Native's tools/gen-benchmark-charts/generate_charts.py so the fonts,
palette, surface, and layout match their published charts exactly — only the data and the
narrative are pocketken's.  Same schema (benchmark-data.json); run with:

    uv run generate_charts.py <benchmark-data.json> <output-dir>

Palette kept identical to theirs (blue = H3.NET.Native, orange = pocketken.H3) so these read
as the same chart family with the numbers flipped; a muted series is added for pocketken 4.0.0.
"""
import json
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.ticker import FuncFormatter

# --- validated palette (identical to the native repo's charts) ------------------
NATIVE = "#2a78d6"   # blue  -> H3.NET.Native
POCKET = "#eb6834"   # orange -> pocketken.H3 (span / current)
STREAM = "#f4a26b"   # light orange -> pocketken.H3 streaming (allocating) API
POCKET40 = "#b6b4ab" # muted -> pocketken.H3 4.0.0 (the version they measured)
SURFACE = "#fcfcfb"
INK = "#0b0b0b"
INK2 = "#52514e"
MUTED = "#898781"
GRID = "#e1e0d9"
AXIS = "#c3c2b7"

plt.rcParams.update({
    "font.family": "sans-serif",
    "font.sans-serif": ["Helvetica Neue", "Helvetica", "Arial", "DejaVu Sans"],
    "figure.facecolor": SURFACE,
    "axes.facecolor": SURFACE,
    "savefig.facecolor": SURFACE,
    "text.color": INK,
    "axes.edgecolor": AXIS,
    "axes.labelcolor": INK2,
    "xtick.color": MUTED,
    "ytick.color": MUTED,
    "xtick.labelsize": 9,
    "ytick.labelsize": 9,
    "axes.linewidth": 0.8,
})

def _prov(data):
    p = data["provenance"]
    cpu = p["cpu"].split(" Processor")[0].split(",")[0]
    return f"{cpu}  ·  {p['dotnet']}  ·  {p['tool']}  ·  Uber H3 v4.5.0  ·  raw libh3 via P/Invoke"


def _bytes(b):
    if b == 0:
        return "0 B"
    if b < 1024:
        return f"{b} B"
    if b < 1024 * 1024:
        return f"{b / 1024:.1f} KB"
    return f"{b / (1024 * 1024):.1f} MB"


def _style(ax):
    ax.spines["top"].set_visible(False)
    ax.spines["right"].set_visible(False)
    ax.spines["left"].set_color(AXIS)
    ax.spines["bottom"].set_color(AXIS)
    ax.tick_params(length=0)


def _title(ax, title, subtitle=""):
    ax.set_title(title, color=INK, fontsize=14, fontweight="bold", loc="left",
                 pad=(44 if subtitle else 16))
    if subtitle:
        ax.text(0, 1.02, subtitle, transform=ax.transAxes, color=INK2, fontsize=10.5,
                va="bottom", ha="left")


def _footer(fig, prov):
    fig.text(0.008, 0.008, prov, color=MUTED, fontsize=7.5, va="bottom", ha="left")


def _save(fig, out_dir, name):
    fig.savefig(f"{out_dir}/{name}.png", dpi=200, bbox_inches="tight", pad_inches=0.28)
    fig.savefig(f"{out_dir}/{name}.svg", bbox_inches="tight", pad_inches=0.28)
    plt.close(fig)


def chart_crossover(data, out_dir):
    rows = data["polygon_sweep"]["rows"]
    cells = [r["cells"] for r in rows]
    native = [r["native_us"] for r in rows]
    pocket = [r["pocketken_us"] for r in rows]
    pocket40 = [r.get("pocketken40_us") for r in rows]
    # ParallelFill only exists / only pays off on large fills, so its points are sparse.
    par_pts = [(r["cells"], r["pocketken_parallel_us"]) for r in rows
               if r.get("pocketken_parallel_us") is not None]

    fig, ax = plt.subplots(figsize=(8.2, 5.0))
    if all(v is not None for v in pocket40):
        ax.plot(cells, pocket40, color=POCKET40, lw=1.8, ls="--", marker="o", ms=5,
                mfc=POCKET40, mec=SURFACE, mew=1.2, label="pocketken.H3 4.0.0", zorder=2)
    ax.plot(cells, native, color=NATIVE, lw=2.2, marker="o", ms=6.5, mfc=NATIVE,
            mec=SURFACE, mew=1.4, label="H3.NET.Native", zorder=3)
    ax.plot(cells, pocket, color=POCKET, lw=2.2, marker="o", ms=6.5, mfc=POCKET,
            mec=SURFACE, mew=1.4, label="pocketken.H3", zorder=4)
    if par_pts:
        px, py = zip(*par_pts)
        ax.plot(px, py, color=POCKET, lw=2.0, ls=":", marker="D", ms=6, mfc=SURFACE,
                mec=POCKET, mew=1.6, label="pocketken.H3 ParallelFill", zorder=5)

    ax.set_xscale("log")
    ax.set_yscale("log")

    ax.grid(axis="both", color=GRID, lw=0.8, zorder=0)
    ax.set_axisbelow(True)
    _style(ax)
    ax.set_xlabel("output cell count  (same polygon, increasing resolution)")
    ax.set_ylabel("time per fill  (microseconds, log)")
    ax.xaxis.set_major_formatter(FuncFormatter(lambda v, _: f"{int(v):,}" if v >= 1 else f"{v:g}"))
    ax.yaxis.set_major_formatter(FuncFormatter(lambda v, _: f"{v:g}"))

    ax.legend(loc="upper left", frameon=False, fontsize=10.5, labelcolor=INK)
    _title(ax, "Polygon fill: time vs output cells")
    _footer(fig, _prov(data))
    _save(fig, out_dir, "no-crossover")


def chart_matrix(data, out_dir):
    from matplotlib.patches import Patch
    from matplotlib.lines import Line2D

    # best-first: sort by the strongest pocketken variant (span, else streaming)
    rows = sorted(data["op_matrix"]["rows"],
                  key=lambda r: r["span_ratio"] if r["span_ratio"] is not None else r["streaming_ratio"])
    ops = [r["op"] for r in rows]
    n = len(ops)
    y = list(range(n))
    off = 0.2
    h = 0.34

    fig, (axT, axA) = plt.subplots(1, 2, figsize=(12.4, 6.6), sharey=True,
                                   gridspec_kw={"wspace": 0.06})

    # --- left: time, ratio vs the native binding -----------------------------------
    tmax = max(r["streaming_ratio"] or 0 for r in rows) * 1.18
    for i, r in zip(y, rows):
        if r["streaming_ratio"] is not None:
            axT.barh(i - off, r["streaming_ratio"], height=h, color=STREAM, zorder=3)
            axT.text(r["streaming_ratio"] + 0.015, i - off, f"{r['streaming_ratio']:.2f}x",
                     va="center", ha="left", color=INK2, fontsize=8.5)
        if r["span_ratio"] is not None:
            axT.barh(i + off, r["span_ratio"], height=h, color=POCKET, zorder=3)
            axT.text(r["span_ratio"] + 0.015, i + off, f"{r['span_ratio']:.2f}x",
                     va="center", ha="left", color=INK, fontsize=8.5)
    axT.axvline(1.0, color=NATIVE, lw=1.3, ls="--", zorder=2)
    axT.set_xlim(0, tmax)
    axT.set_xlabel("time vs native binding  (x, lower is better)")
    axT.set_title("Speed", color=INK, fontsize=12, fontweight="bold", loc="left", pad=10)
    axT.set_yticks(y)
    axT.set_yticklabels(ops, color=INK2, fontsize=10)
    axT.invert_yaxis()
    axT.grid(axis="x", color=GRID, lw=0.8, zorder=0)
    axT.set_axisbelow(True)
    _style(ax=axT)

    # --- right: allocation, bytes (log) --------------------------------------------
    floor = 16
    amax = max(max(r["native_alloc_b"] or 0, r["pocketken_streaming_alloc_b"] or 0) for r in rows)
    for i, r in zip(y, rows):
        sa, spa, na = r["pocketken_streaming_alloc_b"], r["pocketken_span_alloc_b"], r["native_alloc_b"]
        if sa is not None:
            axA.barh(i - off, max(sa, floor), height=h, color=STREAM, zorder=3)
            axA.text(max(sa, floor) * 1.15, i - off, _bytes(sa), va="center", ha="left",
                     color=INK2, fontsize=8)
        if spa is not None:
            axA.barh(i + off, max(spa, floor), height=h, color=POCKET, zorder=3)
            axA.text(max(spa, floor) * 1.15, i + off, _bytes(spa), va="center", ha="left",
                     color=INK, fontsize=8)
        if na:
            axA.plot([na], [i], marker="|", color=NATIVE, ms=17, mew=2.4, zorder=4)
    axA.set_xscale("log")
    axA.set_xlim(floor, amax * 6)
    axA.set_xlabel("bytes allocated  (log, lower is better)")
    axA.set_title("Allocation", color=INK, fontsize=12, fontweight="bold", loc="left", pad=10)
    axA.grid(axis="x", color=GRID, lw=0.8, zorder=0)
    axA.set_axisbelow(True)
    _style(ax=axA)

    legend = [
        Patch(facecolor=STREAM, label="pocketken.H3 streaming"),
        Patch(facecolor=POCKET, label="pocketken.H3 span (zero-alloc)"),
        Line2D([0], [0], color=NATIVE, lw=1.6, ls="--", marker="|", ms=10, mew=2,
               label="H3.NET.Native"),
    ]
    fig.legend(handles=legend, loc="lower center", ncol=3, frameon=False,
               fontsize=10, labelcolor=INK, bbox_to_anchor=(0.5, -0.015))
    fig.suptitle("pocketken.H3 vs the native binding, per operation",
                 color=INK, fontsize=14, fontweight="bold", x=0.012, ha="left", y=1.02)
    _save(fig, out_dir, "across-the-board")


def chart_overhead(data, out_dir):
    rows = {(r["op"], r["impl"]): r for r in data["small_workloads"]["rows"]}
    ops = ["LatLngToCell", "GridDisk"]
    native = [rows[(o, "H3.NET.Native")]["ratio"] for o in ops]
    pocket = [rows[(o, "pocketken.H3")]["ratio"] for o in ops]

    x = range(len(ops))
    w = 0.34
    fig, ax = plt.subplots(figsize=(7.4, 4.8))
    b1 = ax.bar([i - w / 2 - 0.012 for i in x], native, w, color=NATIVE, label="H3.NET.Native", zorder=3)
    b2 = ax.bar([i + w / 2 + 0.012 for i in x], pocket, w, color=POCKET, label="pocketken.H3", zorder=3)

    ax.axhline(1.0, color=AXIS, lw=1.1, zorder=2)
    ax.text(0.5, 1.012, "raw libh3 (P/Invoke from .NET)", color=INK2, fontsize=8.5, va="bottom", ha="center")

    for bars in (b1, b2):
        for rect in bars:
            ax.text(rect.get_x() + rect.get_width() / 2, rect.get_height() + 0.02,
                    f"{rect.get_height():.2f}x", ha="center", va="bottom", color=INK, fontsize=9.5)

    ax.set_xticks(list(x))
    ax.set_xticklabels(ops, color=INK2, fontsize=10.5)
    ax.set_ylim(0, max(native + pocket) * 1.18)
    ax.grid(axis="y", color=GRID, lw=0.8, zorder=0)
    ax.set_axisbelow(True)
    _style(ax)
    ax.set_ylabel("time relative to raw libh3  (lower is better)")
    ax.legend(loc="upper left", frameon=False, fontsize=10.5, labelcolor=INK)
    _title(ax, "Indexing and traversal vs libh3 P/Invoke")
    _footer(fig, _prov(data))
    _save(fig, out_dir, "overhead-vs-raw")


def chart_allocation(data, out_dir):
    rows = {(r["op"], r["impl"]): r for r in data["small_workloads"]["rows"]}
    ops = ["LatLngToCell", "GridDisk", "Polyfill 22k", "Polyfill 7.7M"]
    native = [rows[(o, "H3.NET.Native")]["alloc_b"] for o in ops]
    pocket = [rows[(o, "pocketken.H3")]["alloc_b"] for o in ops]

    x = range(len(ops))
    w = 0.34
    fig, ax = plt.subplots(figsize=(7.8, 4.8))
    floor = 60  # log-y visual floor; true zero is annotated, not drawn
    b1 = ax.bar([i - w / 2 - 0.012 for i in x], [max(v, floor) for v in native], w,
                color=NATIVE, label="H3.NET.Native", zorder=3)
    b2 = ax.bar([i + w / 2 + 0.012 for i in x], [max(v, floor) for v in pocket], w,
                color=POCKET, label="pocketken.H3", zorder=3)

    ax.set_yscale("log")
    for bars, vals in ((b1, native), (b2, pocket)):
        for rect, v in zip(bars, vals):
            ax.text(rect.get_x() + rect.get_width() / 2, rect.get_height() * 1.08,
                    _bytes(v), ha="center", va="bottom", color=INK, fontsize=9.5)

    ax.set_xticks(list(x))
    ax.set_xticklabels(ops, color=INK2, fontsize=10.5)
    ax.set_ylim(floor, max(native + pocket) * 3)
    ax.grid(axis="y", color=GRID, lw=0.8, zorder=0)
    ax.set_axisbelow(True)
    _style(ax)
    ax.set_ylabel("bytes allocated per op  (log, lower is better)")
    ax.legend(loc="upper left", frameon=False, fontsize=10.5, labelcolor=INK)
    _title(ax, "Managed allocation: flat, often zero")
    _footer(fig, _prov(data))
    _save(fig, out_dir, "allocation")


def main():
    data_path, out_dir = sys.argv[1], sys.argv[2]
    with open(data_path) as f:
        data = json.load(f)
    chart_crossover(data, out_dir)
    chart_matrix(data, out_dir)
    chart_overhead(data, out_dir)
    chart_allocation(data, out_dir)
    print(f"wrote no-crossover, across-the-board, overhead-vs-raw, allocation (.png + .svg) to {out_dir}")


if __name__ == "__main__":
    main()
