# H3.CompareBenchmarks

Standalone benchmark suite comparing the published `pocketken.H3` 4.0.0 nuget
package against the current working tree, covering indexing, inspection,
traversal, hierarchy, sets, edges, areas and polyfill.

Because 4.0.0 (class-based `H3Index`) and the current tree (struct-based) are
not ABI compatible, the shared benchmark sources are compiled into two
separate variant projects rather than using BenchmarkDotNet's NuGet job
feature:

```sh
# benchmark the published 4.0.0 package
dotnet run --configuration Release --project benchmarks/H3.CompareBenchmarks/v4.0.0 -- --filter '*' --join

# benchmark the current working tree
dotnet run --configuration Release --project benchmarks/H3.CompareBenchmarks/current -- --filter '*' --join
```

Each run emits a table (and artifacts under `BenchmarkDotNet.Artifacts/`)
tagged with the job id `4.0.0` or `current`; compare the two.  The
MemoryDiagnoser `Allocated` column is deterministic and is the most reliable
comparison on shared/noisy hosts, where timings may vary between runs.
