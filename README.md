<img align="right" src="https://uber.github.io/img/h3Logo-color.svg" alt="H3 Logo" width="200">

# H3.net: A port of Uber's Hexagonal Hierarchical Geospatial Indexing System to C#
This is a port of [Uber's H3 library](https://github.com/uber/h3) to C#, with additional functionality to support [NetTopologySuite](http://nettopologysuite.github.io/NetTopologySuite/index.html) geometries.  It is based on release 3.7.2 of the library.

H3 is a geospatial indexing system using a hexagonal grid that can be (approximately) subdivided into finer and finer hexagonal grids, combining the benefits of a hexagonal grid with [S2](https://code.google.com/archive/p/s2-geometry-library/)'s hierarchical subdivisions.

Upstream documentation is available at [https://h3geo.org/](https://h3geo.org/).  Documentation for this port of the library is unfortunately currently limited to documentation in the source and examples via unit tests.

## Work in Progress!
This is a work in progress and likely contains the odd silly bug or poorly performing/confusing implementation choice (PR's are welcome!).  I have tried to make the library work and feel more "C# like" using operators, extension methods, enumerables vs. arrays and so on, but some things may be implemented more or less as straight ports of the upstream library.  Other than the obvious difference in some method names and whatnot, the biggest difference vs. the upstream library is the reliance on NTS for geometries (e.g. cell boundary polygons) and algorithms such as polyfill instead of in-library classes such as the `GeoBoundary` and vertex graph stuff.

While the majority of the core H3 API should be here in one form or another, there's still gaps particularly in terms of documentation and tests to validate behaviour vs. upstream.  My focus so far has been on getting the algorithm side of things (such as k-rings, polyfill and so on) working and tested as I needed those features for the project(s) I am working on, however given that the majority of those methods depend on the basics of the library working, test coverage is "ok".  I also make use of the [PostgreSQL bindings](https://github.com/bytesandbrains/h3-pg) in my work, so I tend to validate results with that as well.

PRs to improve code, tests and documentation are definitely welcome, although please keep in mind I am quite busy these days and may be a bit slow to respond.  Sorry in advance!

## Installing
Available on [nuget.org](https://nuget.org) as [pocketken.H3](https://www.nuget.org/packages/pocketken.H3/).

```
PM> Install-Package pocketken.H3 -Version 3.7.2
```

See [CHANGES.md](CHANGES.md) for a list of changes between releases.

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

### GetParentForResolution
Using `89283080dcbffff` (Uber's SF Test index @ resolution 9) to get parent at resolution 0 (a silly microbenchmark):

|                              Method |      Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------------ |----------:|----------:|----------:|-------:|------:|------:|----------:|
| pocketken.H3.GetParentForResolution |  4.458 ns | 0.0474 ns | 0.0443 ns | 0.0029 |     - |     - |      24 B |
|                      H3Lib.ToParent | 20.817 ns | 0.1196 ns | 0.1118 ns |      - |     - |     - |         - |

#### GetChildrenForResolution
Using `89283080dcbffff` (Uber's SF Test index @ resolution 9) to get all children at resolution 15.

|                                Method |     Mean |     Error |    StdDev |     Gen 0 |     Gen 1 |    Gen 2 | Allocated |
|-------------------------------------- |---------:|----------:|----------:|----------:|----------:|---------:|----------:|
| pocketken.H3.GetChildrenForResolution | 9.128 ms | 0.1809 ms | 0.2415 ms |  796.8750 |  781.2500 | 484.3750 |   4.69 MB |
|                      H3Lib.ToChildren | 9.671 ms | 0.1904 ms | 0.2266 ms | 3453.1250 | 1671.8750 | 984.3750 |  23.55 MB |

### Algorithms

#### Fill (Polyfill)
Filling world and [Uber SF Test](https://github.com/uber/h3/blob/master/src/apps/testapps/testPolygonToCells.c#L27) polygons at varied resolutions:

* world, res 4: 288,122 cells
* world, res 5: 2,016,842 cells
* SF test, res 10: 8,794 cells
* SF test, res 11: 61,569 cells
* SF test, res 12: 430,832 cells
* SF test, res 13: 3,015,836 cells
* SF test, res 14: 21,111,191 cells
* SF test, res 15: 147,778,335 cells

|                               Method |           Mean |       Error |      StdDev |        Gen 0 |       Gen 1 |      Gen 2 | Allocated |
|------------------------------------- |---------------:|------------:|------------:|-------------:|------------:|-----------:|----------:|
| 'pocketken.H3.Fill(worldPolygon, 4)' |     203.042 ms |   1.2783 ms |   1.1331 ms |   10000.0000 |   2000.0000 |  1000.0000 |     90 MB |
| 'pocketken.H3.Fill(worldPolygon, 5)' |     1,461.0 ms |    12.00 ms |    10.02 ms |   71000.0000 |  13000.0000 |  4000.0000 |    648 MB |
|   'pocketken.H3.Fill(sfPolygon, 10)' |       7.603 ms |   0.0165 ms |   0.0146 ms |     367.1875 |    226.5625 |   117.1875 |      3 MB |
|      'H3Lib.Polyfill(sfPolygon, 10)' |     494.078 ms |   1.2105 ms |   0.9450 ms |    3000.0000 |           - |          - |     27 MB |
|   'pocketken.H3.Fill(sfPolygon, 11)' |      51.196 ms |   0.8691 ms |   0.8129 ms |    2800.0000 |   1200.0000 |   700.0000 |     20 MB |
|      'H3Lib.Polyfill(sfPolygon, 11)' |   3,319.726 ms |   3.1163 ms |   2.9150 ms |   21000.0000 |   3000.0000 |  1000.0000 |    168 MB |
|   'pocketken.H3.Fill(sfPolygon, 12)' |     375.414 ms |   5.0360 ms |   4.7107 ms |   16000.0000 |   3000.0000 |  2000.0000 |    145 MB |
|      'H3Lib.Polyfill(sfPolygon, 12)' |  20,111.706 ms | 269.9403 ms | 239.2950 ms |  137000.0000 |  19000.0000 |  2000.0000 |  1,119 MB |
|   'pocketken.H3.Fill(sfPolygon, 13)' |     2,788.0 ms |    10.82 ms |     9.03 ms |  109000.0000 |  30000.0000 |  9000.0000 |  1,046 MB |
|   'pocketken.H3.Fill(sfPolygon, 14)' |  20,150.948 ms | 104.1208 ms |  92.3004 ms |  719000.0000 | 119000.0000 | 10000.0000 |  6,702 MB |
|   'pocketken.H3.Fill(sfPolygon, 15)' | 136,125.551 ms | 505.5441 ms | 472.8863 ms | 5008000.0000 | 801000.0000 | 62000.0000 | 47,576 MB |

#### Lines
Line from `8e283080dc80007` to `8e48e1d7038d527` (`DistanceTo` of 554,625 cells).

|              Method |       Mean |    Error |   StdDev |        Gen 0 |      Gen 1 |     Gen 2 | Allocated |
|-------------------- |-----------:|---------:|---------:|-------------:|-----------:|----------:|----------:|
| pocketken.H3.LineTo |   739.2 ms | 10.19 ms |  9.53 ms |   40000.0000 | 12000.0000 | 1000.0000 |    333 MB |
|        H3Lib.LineTo | 4,709.9 ms | 11.84 ms | 11.07 ms | 1057000.0000 |  3000.0000 | 1000.0000 |  8,449 MB |

#### Rings
`hex` is a hexagon index (`8f48e1d7038d520`).

|                                   Method |       Mean |    Error |   StdDev |    Gen 0 |   Gen 1 |   Gen 2 |  Allocated |
|----------------------------------------- |-----------:|---------:|---------:|---------:|--------:|--------:|-----------:|
|     'pocketken.H3.GetKRing(hex, k = 50)' |   460.1 us |  1.99 us |  1.86 us |  73.7305 | 36.6211 |       - |  607.75 KB |
| 'pocketken.H3.GetKRingFast(hex, k = 50)' |   471.4 us |  2.48 us |  2.32 us |  66.4063 | 33.2031 |       - |  547.92 KB |
| 'pocketken.H3.GetKRingSlow(hex, k = 50)' | 4,852.8 us | 38.70 us | 36.20 us | 179.6875 | 85.9375 | 85.9375 | 1634.09 KB |
|      'H3Lib.KRingDistances(hex, k = 50)' |   381.6 us |  0.87 us |  0.68 us |  99.6094 | 99.6094 | 99.6094 |  486.59 KB |

`pent` is a pentagon index (`8e0800000000007`) which forces the use of the iterative (recursive in the case of H3Lib) method of generating the ring due to the fast method's inability to handle pentagons.

|                                    Method |          Mean |       Error |      StdDev |        Gen 0 |        Gen 1 |        Gen 2 |   Allocated |
|------------------------------------------ |--------------:|------------:|------------:|-------------:|-------------:|-------------:|------------:|
|     'pocketken.H3.GetKRing(pent, k = 50)' |      4.088 ms |   0.0263 ms |   0.0246 ms |     179.6875 |      85.9375 |      85.9375 |      1.6 MB |
| 'pocketken.H3.GetKRingSlow(pent, k = 50)' |      3.985 ms |   0.0138 ms |   0.0115 ms |     179.6875 |      85.9375 |      85.9375 |      1.6 MB |
|      'H3Lib.KRingDistances(pent, k = 50)' | 59,216.102 ms | 960.4448 ms | 898.4006 ms | 7692000.0000 | 6112000.0000 | 5064000.0000 | 71358.66 MB |

### Sets
* Compact: Result of compacting all cells at resolution 5.
* Uncompact: Result of uncompacting all base cells to resolution of 5.

|                 Method |     Mean |   Error |  StdDev |      Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|----------------------- |---------:|--------:|--------:|-----------:|----------:|----------:|----------:|
|   pocketken.H3.Compact | 330.3 ms | 5.50 ms | 5.14 ms | 11000.0000 | 3000.0000 |         - | 243.51 MB |
|          H3Lib.Compact | 381.0 ms | 5.83 ms | 5.45 ms |  9000.0000 | 4000.0000 | 2000.0000 | 305.24 MB |
| pocketken.H3.Uncompact | 136.6 ms | 0.36 ms | 0.32 ms |  6500.0000 | 3500.0000 | 1000.0000 |  78.18 MB |
|        H3Lib.Uncompact | 203.3 ms | 4.05 ms | 4.16 ms | 43000.0000 | 7333.3333 |  666.6667 | 493.02 MB |
