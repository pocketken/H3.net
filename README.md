<img align="right" src="https://uber.github.io/img/h3Logo-color.svg" alt="H3 Logo" width="200">

# H3.net: A port of Uber's Hexagonal Hierarchical Geospatial Indexing System to C#
This is a port of [Uber's H3 library](https://github.com/uber/h3) to C#, with additional functionality to support [NetTopologySuite](http://nettopologysuite.github.io/NetTopologySuite/index.html) geometries.  It is based on release 3.7.1 of the library.

H3 is a geospatial indexing system using a hexagonal grid that can be (approximately) subdivided into finer and finer hexagonal grids, combining the benefits of a hexagonal grid with [S2](https://code.google.com/archive/p/s2-geometry-library/)'s hierarchical subdivisions.

Upstream documentation is available at [https://h3geo.org/](https://h3geo.org/).  Documentation for this port of the library is unfortunately currently limited to documentation in the source and examples via unit tests.

## Work in Progress!
This is a work in progress and likely contains some silly bugs and poorly performing or just plain confusing implementation choices.  I have tried to make the library work and feel more "C# like" using operators, extension methods, enumerables vs. arrays and so on, but some things may be implemented more or less as straight ports of the upstream library.  Other than the obvious difference in some method names and whatnot, the biggest difference vs. the upstream library is the reliance on NTS for geometries (e.g. cell boundary polygons) and algorithms such as polyfill instead of in-library classes such as the `GeoBoundary` and vertex graph stuff.

While the majority of the core H3 API should be here in one form or another, there's still gaps particularly in terms of documentation and tests to validate behaviour vs. upstream.  My focus so far has been on getting the algorithm side of things (such as k-rings, polyfill and so on) working and tested as I needed those features for the project(s) I am working on, however given that the majority of those methods depend on the basics of the library working, test coverage is "ok".  I also make use of the [PostgreSQL bindings](https://github.com/bytesandbrains/h3-pg) in my work, so I tend to validate results with that as well.

PRs to improve code, tests and documentation are definitely welcome, although please keep in mind I am quite busy these days and may be a bit slow to respond.  Sorry in advance!

## Installing
I haven't published this to Nuget or anything yet, so for now you'll need to check the code out and build the package locally (e.g. `dotnet pack`), and/or add a dependency to this project from an existing solution.  It should compile/work anywhere .NET 5 does (I presently build/use on a variety of projects on Windows, Linux and Mac).

## Some Mostly-Pointless Benchmarks
There is an extremely basic set of benchmarks using [BenchmarkDotNet](https://benchmarkdotnet.org/index.html) that I have begun to use in order to track performance and perform optimizations as things progress.  You can check the code out to run the benchmarks locally if you want, e.g.:

```sh
$ dotnet run --configuration Release --project .\test\H3.Benchmarks\H3.Benchmarks.csproj --filter *Uncompact* --join
```

All numbers here are from my primary Windows development VM:

``` ini
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
AMD Ryzen 9 3900X, 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.201
  [Host]        : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  .NET Core 5.0 : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

Job=.NET Core 5.0  Runtime=.NET Core 5.0
```

While there are some comparisons here against [H3Lib](https://github.com/RichardVasquez/h3net), I still need to work on getting some benchmarks for the other H3 package which wraps the native library; if anyone is interested in assisting PRs are welcome!

### Hierarchy Ops

#### GetChildrenForResolution
Using `89283080dcbffff` (Uber's SF Test index @ resolution 9) to get all children at resolution 15.

|                                Method |     Mean |     Error |    StdDev |     Gen 0 |     Gen 1 |    Gen 2 | Allocated |
|-------------------------------------- |---------:|----------:|----------:|----------:|----------:|---------:|----------:|
| pocketken.H3.GetChildrenForResolution | 9.368 ms | 0.1335 ms | 0.1184 ms |  796.8750 |  781.2500 | 484.3750 |   4.69 MB |
|                      H3Lib.ToChildren | 9.689 ms | 0.1837 ms | 0.1966 ms | 3453.1250 | 1640.6250 | 984.3750 |  23.55 MB |

### Lines
Line from `8e283080dc80007` to `8e48e1d7038d527` (`DistanceTo` of 554,625 cells).

|              Method |       Mean |    Error |  StdDev |        Gen 0 |      Gen 1 |     Gen 2 |  Allocated |
|-------------------- |-----------:|---------:|--------:|-------------:|-----------:|----------:|-----------:|
| pocketken.H3.LineTo |   972.3 ms |  8.73 ms | 7.29 ms |   45000.0000 | 11000.0000 | 1000.0000 |  355.44 MB |
|        H3Lib.LineTo | 4,422.1 ms | 10.23 ms | 9.57 ms | 1057000.0000 |  3000.0000 | 1000.0000 | 8449.31 MB |

### Rings
`hex` is a hexagon index (`8f48e1d7038d520`).

|                                   Method |       Mean |    Error |   StdDev |   Gen 0 |    Gen 1 |   Gen 2 |  Allocated |
|----------------------------------------- |-----------:|---------:|---------:|--------:|---------:|--------:|-----------:|
| 'pocketken.H3.GetKRingFast(hex, k = 50)' |   593.2 us |  4.04 us |  3.78 us |  66.4063 | 33.2031 |       - |  547.92 KB |
| 'pocketken.H3.GetKRingSlow(hex, k = 50)' | 5,846.9 us | 26.14 us | 24.45 us | 179.6875 | 85.9375 | 85.9375 | 1634.09 KB |
|      'H3Lib.KRingDistances(hex, k = 50)' |   377.3 us |  1.83 us |  1.53 us |  99.6094 | 99.6094 | 99.6094 |  486.59 KB |

`pent` is a pentagon index (`8e0800000000007`) which forces the use of the iterative (recursive in the case of H3Lib) method of generating the ring due to the fast method's inability to handle pentagons.

|                                    Method |          Mean |         Error |        StdDev |        Gen 0 |        Gen 1 |        Gen 2 |   Allocated |
|------------------------------------------ |--------------:|--------------:|--------------:|-------------:|-------------:|-------------:|------------:|
| 'pocketken.H3.GetKRingSlow(pent, k = 50)' |      5.644 ms |     0.0249 ms |     0.0233 ms |     179.6875 |      85.9375 |      85.9375 |      1.6 MB |
|      'H3Lib.KRingDistances(pent, k = 50)' | 59,581.867 ms | 1,123.9235 ms | 1,154.1867 ms | 7683000.0000 | 6097000.0000 | 5055000.0000 | 71357.79 MB |

### Sets
* Compact: Result of compacting all base cells at resolution 5.
* Uncompact: Result of uncompacting all base cells to resolution of 5.

|                Type |                 Method |     Mean |   Error |  StdDev |      Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|-------------------- |----------------------- |----------:|-------:|--------:|-----------:|----------:|----------:|---------:|
|   CompactBenchmarks |   pocketken.H3.Compact |  356.1 ms | 6.75 ms | 6.63 ms | 11000.0000 | 3000.0000 |         - | 243.51 MB |
|   CompactBenchmarks |          H3Lib.Compact |  389.1 ms | 6.56 ms | 6.14 ms |  9000.0000 | 4000.0000 | 2000.0000 | 305.24 MB |
| UncompactBenchmarks | pocketken.H3.Uncompact |  138.7 ms | 1.45 ms | 1.36 ms |  6500.0000 | 3500.0000 | 1000.0000 |  78.18 MB |
| UncompactBenchmarks |        H3Lib.Uncompact |  194.9 ms | 1.97 ms | 1.75 ms | 43000.0000 | 7333.3333 |  666.6667 | 493.02 MB |
