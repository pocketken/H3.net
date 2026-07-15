# Benchmarks

Three benchmark projects and the scripts that turn their output into the charts and
tables in [docs/benchmark.md](../docs/benchmark.md). None of this ships in the NuGet
package; it references `src/H3` directly.

| Project | Compares |
| --- | --- |
| `H3.Benchmarks` | assorted micro-benchmarks (some against [H3Lib](https://github.com/RichardVasquez/h3net) and the published `pocketken.H3` package) |
| `H3.CompareBenchmarks` | this working tree against the published `pocketken.H3` 4.0.0 package (see its own README) |
| `H3.NativeCompare` | this working tree against a native `libh3` binding ([H3.NET.Native](https://github.com/FOOincognita/H3.NET.Native)) and a bare `libh3` P/Invoke |

`H3.NativeCompare` needs the `H3.NET.Native` package (it bundles a `libh3` for `linux-x64`)
and so runs on `linux-x64`.

## Running

```sh
# native-binding comparison (net10)
dotnet run -c Release -f net10.0 --project benchmarks/H3.NativeCompare -- --filter '*' --artifacts benchmarks/H3.NativeCompare/run

# a single category / narrower filter, e.g. the polygon sweep
dotnet run -c Release -f net10.0 --project benchmarks/H3.NativeCompare -- --filter '*PolygonSweep*'
```

BenchmarkDotNet writes per-class `*-report.csv` / `*-report-github.md` under the
`--artifacts` dir's `results/` (git-ignored).

## Regenerating the charts and tables

The `charts/` scripts are pure-Python (matplotlib via `uv`). Nothing is hand-typed;
every number is parsed from the BenchmarkDotNet report CSVs.

```sh
# CSVs -> benchmark-data.json
python benchmarks/charts/build_data.py benchmarks/H3.NativeCompare/run/results benchmarks/charts/benchmark-data.json

# benchmark-data.json -> docs/images/*.png + *.svg
uv run benchmarks/charts/generate_charts.py benchmarks/charts/benchmark-data.json docs/images

# benchmark-data.json -> markdown tables (stdout, or a file)
python benchmarks/charts/generate_tables.py benchmarks/charts/benchmark-data.json
```

`benchmark-data.json` is the committed snapshot the charts and tables are built from, so
the images can be regenerated without re-running the benchmarks.
