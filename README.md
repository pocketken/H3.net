<img align="right" src="https://uber.github.io/img/h3Logo-color.svg" alt="H3 Logo" width="200">

# H3.net: A port of Uber's Hexagonal Hierarchical Geospatial Indexing System to C#
This is a port of Uber's H3 library to C#, with additional functionality to support [NetTopologySuite](http://nettopologysuite.github.io/NetTopologySuite/index.html) geometries.  It is based on release 3.7.1 of the library.

H3 is a geospatial indexing system using a hexagonal grid that can be (approximately) subdivided into finer and finer hexagonal grids, combining the benefits of a hexagonal grid with [S2](https://code.google.com/archive/p/s2-geometry-library/)'s hierarchical subdivisions.

Upstream documentation is available at [https://h3geo.org/](https://h3geo.org/).  Documentation for this port of the library is unfortunately currently limited to documentation in the source and examples via unit tests.

## Work in Progress!
This is a work in progress and likely contains some silly bugs and poorly performing or just plain confusing implementation choices.  While I tried to make the library work and feel more "C# like" using extension methods, enumerables vs. arrays and so on, some things (e.g. `compact`/`uncompact`) are implemented more or less as straight ports of the upstream library, mostly because I haven't spent any time looking at any different way of implementing them.  Other than the obvious difference in some method names and whatnot, the biggest difference vs. the upstream library is the reliance on NTS for geometries (e.g. cell boundary polygons) and algorithms such as polyfill instead of in-library classes such as the `GeoBoundary` and vertex graph stuff.

While the majority of the core H3 API should be here in one form or another, there's still gaps particularly in terms of documentation and tests to validate behaviour vs. upstream.  My focus so far has been on getting the algorithm side of things (such as k-rings, polyfill and so on) working and tested as I needed those features for the project(s) I am working on, however given that the majority of those methods depend on the basics of the library working, test coverage is "ok".  I also make use of the [PostgreSQL bindings](https://github.com/bytesandbrains/h3-pg) in my work, so I tend to validate results with that as well.

PRs to improve tests and documentation are definitely welcome, although please keep in mind I am quite busy these days and may be a bit slow to respond.  Sorry in advance!

## Installing
I haven't published this to Nuget or anything yet, so for now you'll need to check the code out and build the package locally (e.g. `dotnet pack`), and/or add a dependency to this project from an existing solution.  It should compile/work anywhere .NET 5 does (I presently build/use on a variety of projects on Windows, Linux and Mac).
