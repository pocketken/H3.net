<img align="right" src="https://uber.github.io/img/h3Logo-color.svg" alt="H3 Logo" width="200">

# H3: A Hexagonal Hierarchical Geospatial Indexing System - Managed C# Port
H3 is a geospatial indexing system using a hexagonal grid that can be (approximately) subdivided into finer and finer hexagonal grids, combining the benefits of a hexagonal grid with [S2](https://code.google.com/archive/p/s2-geometry-library/)'s hierarchical subdivisions.

Documentation is available at [https://h3geo.org/](https://h3geo.org/).

## Work in Progress!
This is a WIP managed port of Uber's H3 library to C#, with additional functionality to support [NetTopologySuite](http://nettopologysuite.github.io/NetTopologySuite/index.html) geometries.

## Installing
You'll need to check the code out and build the nuget package locally (e.g. `dotnet pack`), and/or add a dependency to this project from an existing solution.
