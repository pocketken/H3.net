<img align="right" src="https://uber.github.io/img/h3Logo-color.svg" alt="H3 Logo" width="200">

# H3.net: A port of Uber's Hexagonal Hierarchical Geospatial Indexing System to C#
This is a port of [Uber's H3 library](https://github.com/uber/h3) to C# with most of the polygon functionality based on [NetTopologySuite](http://nettopologysuite.github.io/NetTopologySuite/index.html) instead of in-library implementations.  It supports `netstandard2.0`, `netstandard2.1`, `net6.0`, as well as `net7.0` and is (mostly) based on release 4.1.0 of the C library.

H3 is a geospatial indexing system using a hexagonal grid that can be (approximately) subdivided into finer and finer hexagonal grids, combining the benefits of a hexagonal grid with [S2](https://code.google.com/archive/p/s2-geometry-library/)'s hierarchical subdivisions.

## Installing
Available on [nuget.org](https://nuget.org) as [pocketken.H3](https://www.nuget.org/packages/pocketken.H3/).

```
PM> Install-Package pocketken.H3 -Version 4.1.0
```

See [CHANGES.md](https://github.com/pocketken/H3.net/blob/main/CHANGES.md) for a list of changes between releases.

## Documentation
Upstream documentation is available at [https://h3geo.org/](https://h3geo.org/).  Basic getting-started documentation for this port of the library is available [here](https://github.com/pocketken/H3.net/blob/main/docs/basic-usage.md).  I have tried to make the library work and feel more "C# like" using classes, operators, extension methods, enumerables vs. arrays and so on, but some things may be implemented more or less as straight ports of the upstream library.

While the majority of the core H3 API should be here in one form or another, there's still the odd gap in terms of documentation and tests to validate behaviour vs. upstream.  PRs to improve code, tests and documentation are definitely welcome and appreciated, although please keep in mind I am quite busy these days and may be a bit slow to respond.  Sorry in advance!

## Benchmarks
See these likely-out-of-date [benchmarks](https://github.com/pocketken/H3.net/blob/main/docs/benchmarks.md), or you can run  them yourself - e.g.:

```sh
dotnet run --configuration Release --project .\test\H3.Benchmarks\H3.Benchmarks.csproj --join --framework net6.0
```
