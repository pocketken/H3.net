<img align="right" src="https://uber.github.io/img/h3Logo-color.svg" alt="H3 Logo" width="200">

# H3.net: A port of Uber's Hexagonal Hierarchical Geospatial Indexing System to C#
This is a port of [Uber's H3 library](https://github.com/uber/h3) to C# with most of the polygon functionality based on [NetTopologySuite](http://nettopologysuite.github.io/NetTopologySuite/index.html) instead of in-library implementations.  It supports `netstandard2.0`, `netstandard2.1`, `net8.0` and `net10.0`, and is (mostly) based on release 4.5.0 of the C library.

H3 is a geospatial indexing system using a hexagonal grid that can be (approximately) subdivided into finer and finer hexagonal grids, combining the benefits of a hexagonal grid with [S2](https://code.google.com/archive/p/s2-geometry-library/)'s hierarchical subdivisions.

## Installing
Available on [nuget.org](https://nuget.org) as [pocketken.H3](https://www.nuget.org/packages/pocketken.H3/).

```
PM> Install-Package pocketken.H3 -Version 4.5.0
```

See [CHANGES.md](https://github.com/pocketken/H3.net/blob/main/CHANGES.md) for a list of changes between releases.

### Unity
Use the compiled `netstandard2.1` (or `netstandard2.0`) assembly from the nuget package, e.g. via [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity).  As of 4.5.0 the netstandard targets depend only on `NetTopologySuite` (plus `Microsoft.Bcl.HashCode` for `netstandard2.0`) — there is no `System.Text.Json` dependency chain to fight with, and the library contains no unsafe code or runtime code generation, so it should be IL2CPP/AOT friendly.  `H3IndexJsonConverter` is only available on the `net8.0`+ targets.  Note that consuming the library as *source* in Unity is not supported (it relies on a Roslyn source generator and current C# language features); consume the compiled assembly instead.

## Documentation
Upstream documentation is available at [https://h3geo.org/](https://h3geo.org/).  Basic getting-started documentation for this port of the library is available [here](https://github.com/pocketken/H3.net/blob/main/docs/basic-usage.md).  I have tried to make the library work and feel more "C# like" using value types, operators, extension methods, enumerables vs. arrays and so on, but some things may be implemented more or less as straight ports of the upstream library.

While the majority of the core H3 API should be here in one form or another, there's still the odd gap in terms of documentation and tests to validate behaviour vs. upstream.  PRs to improve code, tests and documentation are definitely welcome and appreciated, although please keep in mind I am quite busy these days and may be a bit slow to respond.  Sorry in advance!

## Contributing
The model types (`H3Index`, `CoordIJK`, `FaceIJK`, `LatLng`, ...) are *mutable structs*: mutating methods modify the receiver in place and return a copy for value-flow usage.  Never chain mutating calls unless you consume the result — `x.DownAperture7Clockwise().ToNeighbour(d);` applies the second mutation to a discarded temporary copy; write sequential statements instead.  Pure members are annotated `readonly`.

## Benchmarks
See these likely-out-of-date [benchmarks](https://github.com/pocketken/H3.net/blob/main/docs/benchmarks.md), or you can run  them yourself - e.g.:

```sh
dotnet run --configuration Release --project .\test\H3.Benchmarks\H3.Benchmarks.csproj --join --framework net10.0
```

To compare the current working tree against the published 4.0.0 package, see [test/H3.CompareBenchmarks](https://github.com/pocketken/H3.net/blob/main/test/H3.CompareBenchmarks/README.md).
