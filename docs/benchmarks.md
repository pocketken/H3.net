## Some Mostly-Pointless Benchmarks
There is an extremely basic set of benchmarks using [BenchmarkDotNet](https://benchmarkdotnet.org/index.html) that I have begun to use in order to track performance and perform optimizations as things progress.  You can check the code out to run the benchmarks locally if you want, e.g.:

```sh
$ dotnet run --configuration Release --project .\test\H3.Benchmarks\H3.Benchmarks.csproj --filter *Uncompact* --join --framework net5.0
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

#### GetParentForResolution
Using `89283080dcbffff` (Uber's SF Test index @ resolution 9) to get parent at resolution 0 (a silly microbenchmark):

|                              Method |      Mean |     Error |    StdDev |  Gen 0 | Allocated |
|------------------------------------ |----------:|----------:|----------:|-------:|----------:|
| pocketken.H3.GetParentForResolution |  4.918 ns | 0.0838 ns | 0.0784 ns | 0.0029 |      24 B |
|                      H3Lib.ToParent | 21.087 ns | 0.1255 ns | 0.1174 ns |      - |         - |

#### GetChildrenForResolution
Using `89283080dcbffff` (Uber's SF Test index @ resolution 9) to get all children at resolution 15.

|                                Method |      Mean |     Error |    StdDev |     Gen 0 |     Gen 1 |    Gen 2 | Allocated |
|-------------------------------------- |----------:|----------:|----------:|----------:|----------:|---------:|----------:|
| pocketken.H3.GetChildrenForResolution |  9.639 ms | 0.1317 ms | 0.1099 ms |  796.8750 |  781.2500 | 484.3750 |      5 MB |
|                      H3Lib.ToChildren | 10.660 ms | 0.2096 ms | 0.3072 ms | 3453.1250 | 1671.8750 | 984.3750 |     24 MB |

#### GetDirectNeighbour
Using `89283080dcbffff` (Uber's SF Test index @ resolution 9) and `8e0800000000007` (first pentagon @ resolution 14) to get neighbours at `Direction.I` and `Direction.IJ`:

|                                      Method |     Mean |    Error |   StdDev |  Gen 0 | Allocated |
|-------------------------------------------- |---------:|---------:|---------:|-------:|----------:|
|   'pocketken.H3.GetDirectNeighbour(hex, I)' | 16.68 ns | 0.140 ns | 0.124 ns | 0.0029 |      24 B |
|           'H3Lib.NeighborRotations(hex, I)' | 16.70 ns | 0.187 ns | 0.166 ns |      - |         - |
|  'pocketken.H3.GetDirectNeighbour(hex, IJ)' | 24.47 ns | 0.223 ns | 0.198 ns | 0.0029 |      24 B |
|          'H3Lib.NeighborRotations(hex, IJ)' | 27.64 ns | 0.226 ns | 0.189 ns |      - |         - |
|  'pocketken.H3.GetDirectNeighbour(pent, I)' | 27.50 ns | 0.307 ns | 0.287 ns | 0.0029 |      24 B |
|          'H3Lib.NeighborRotations(pent, I)' | 33.15 ns | 0.415 ns | 0.388 ns |      - |         - |
| 'pocketken.H3.GetDirectNeighbour(pent, IJ)' | 27.34 ns | 0.367 ns | 0.343 ns | 0.0029 |      24 B |
|         'H3Lib.NeighborRotations(pent, IJ)' | 32.86 ns | 0.250 ns | 0.234 ns |      - |         - |

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
| 'pocketken.H3.Fill(worldPolygon, 4)' |     196.743 ms |   1.8058 ms |   1.6008 ms |   10000.0000 |   2000.0000 |  1000.0000 |     90 MB |
| 'pocketken.H3.Fill(worldPolygon, 5)' |   1,413.311 ms |  11.3172 ms |  10.5861 ms |   71000.0000 |  13000.0000 |  4000.0000 |    648 MB |
|   'pocketken.H3.Fill(sfPolygon, 10)' |       7.319 ms |   0.0241 ms |   0.0202 ms |     367.1875 |    226.5625 |   117.1875 |      3 MB |
|      'H3Lib.Polyfill(sfPolygon, 10)' |     494.078 ms |   1.2105 ms |   0.9450 ms |    3000.0000 |           - |          - |     27 MB |
|   'pocketken.H3.Fill(sfPolygon, 11)' |      48.994 ms |   0.8197 ms |   0.7667 ms |    2727.2727 |   1090.9091 |   636.3636 |     20 MB |
|      'H3Lib.Polyfill(sfPolygon, 11)' |   3,319.726 ms |   3.1163 ms |   2.9150 ms |   21000.0000 |   3000.0000 |  1000.0000 |    168 MB |
|   'pocketken.H3.Fill(sfPolygon, 12)' |     361.273 ms |   6.7138 ms |   6.2801 ms |   16000.0000 |   3000.0000 |  2000.0000 |    145 MB |
|      'H3Lib.Polyfill(sfPolygon, 12)' |  20,111.706 ms | 269.9403 ms | 239.2950 ms |  137000.0000 |  19000.0000 |  2000.0000 |  1,119 MB |
|   'pocketken.H3.Fill(sfPolygon, 13)' |   2,692.485 ms |  14.1803 ms |  13.2643 ms |  109000.0000 |  30000.0000 |  9000.0000 |  1,046 MB |
|   'pocketken.H3.Fill(sfPolygon, 14)' |  18,216.525 ms |  22.5448 ms |  18.8259 ms |  719000.0000 | 119000.0000 | 10000.0000 |  6,702 MB |
|   'pocketken.H3.Fill(sfPolygon, 15)' | 128,363.156 ms | 501.3173 ms | 444.4047 ms | 4991000.0000 | 791000.0000 | 46000.0000 | 47,576 MB |

#### Lines
Line from `8e283080dc80007` to `8e48e1d7038d527` (`DistanceTo` of 554,625 cells).

|              Method |       Mean |    Error |   StdDev |        Gen 0 |      Gen 1 |     Gen 2 | Allocated |
|-------------------- |-----------:|---------:|---------:|-------------:|-----------:|----------:|----------:|
| pocketken.H3.LineTo |   725.4 ms |  9.40 ms |  8.79 ms |   34000.0000 | 10000.0000 | 1000.0000 |    283 MB |
|        H3Lib.LineTo | 4,683.3 ms | 14.94 ms | 13.25 ms | 1057000.0000 |  3000.0000 | 1000.0000 |  8,449 MB |

#### Rings
`hex` is a hexagon index (`8f48e1d7038d520`).

|                                    Method |            Mean |         Error |        StdDev |        Gen 0 |        Gen 1 |        Gen 2 |     Allocated |
|------------------------------------------ |----------------:|--------------:|--------------:|-------------:|-------------:|-------------:|--------------:|
|      'pocketken.H3.GetKRing(hex, k = 50)' |        419.5 us |       1.79 us |       1.59 us |      73.7305 |      36.6211 |            - |        608 KB |
|  'pocketken.H3.GetKRingFast(hex, k = 50)' |        422.6 us |       1.40 us |       1.09 us |      66.4063 |      33.2031 |            - |        548 KB |
|  'pocketken.H3.GetKRingSlow(hex, k = 50)' |      3,080.5 us |       7.55 us |       6.31 us |     269.5313 |     179.6875 |      89.8438 |      2,113 KB |
|       'H3Lib.KRingDistances(hex, k = 50)' |        463.7 us |       3.11 us |       2.76 us |      99.6094 |      99.6094 |      99.6094 |        487 KB |

`pent` is a pentagon index (`8e0800000000007`) which forces the use of the iterative (recursive in the case of H3Lib) method of generating the ring due to the fast method's inability to handle pentagons.

|                                    Method |            Mean |         Error |        StdDev |        Gen 0 |        Gen 1 |        Gen 2 |     Allocated |
|------------------------------------------ |----------------:|--------------:|--------------:|-------------:|-------------:|-------------:|--------------:|
|     'pocketken.H3.GetKRing(pent, k = 50)' |      3,295.9 us |      24.51 us |      21.73 us |     269.5313 |     179.6875 |      89.8438 |      2,113 KB |
| 'pocketken.H3.GetKRingSlow(pent, k = 50)' |      3,097.6 us |      23.57 us |      22.05 us |     269.5313 |     179.6875 |      89.8438 |      2,113 KB |
|      'H3Lib.KRingDistances(pent, k = 50)' | 79,416,403.4 us | 594,028.76 us | 555,654.87 us | 7644000.0000 | 6050000.0000 | 5015000.0000 | 73,068,645 KB |

### Sets
* Compact: Result of compacting all cells at resolution 5.
* Uncompact: Result of uncompacting all base cells to resolution of 5.

|                 Method |     Mean |   Error |  StdDev |      Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|----------------------- |---------:|--------:|--------:|-----------:|----------:|----------:|----------:|
|   pocketken.H3.Compact | 330.3 ms | 5.50 ms | 5.14 ms | 11000.0000 | 3000.0000 |         - | 243.51 MB |
|          H3Lib.Compact | 381.0 ms | 5.83 ms | 5.45 ms |  9000.0000 | 4000.0000 | 2000.0000 | 305.24 MB |
| pocketken.H3.Uncompact | 136.6 ms | 0.36 ms | 0.32 ms |  6500.0000 | 3500.0000 | 1000.0000 |  78.18 MB |
|        H3Lib.Uncompact | 203.3 ms | 4.05 ms | 4.16 ms | 43000.0000 | 7333.3333 |  666.6667 | 493.02 MB |
