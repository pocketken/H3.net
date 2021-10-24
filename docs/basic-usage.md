# Basic Usage / Quick Start
Here's some (really) quick and dirty examples to get you started with using the library, broken down in (more or less) the same format as the [upstream API documentation](https://h3geo.org/docs/api/indexing).  Note that these examples assume you've imported `H3.Algorithms` and `H3.Extensions`, i.e.:

```cs
using H3;
using H3.Algorithms;
using H3.Extensions;
```

Ustream C function names are also provided for reference.

* [Indexing functions](api-indexing.md) - from/to `string` and `ulong`; creating an index from a geospatial coordinate; getting the centroid of an index
* [Index inspection functions](api-inspection.md) - get an indexes resolution, base cell, validity, whether it's a pentagon
* [Grid traversal functions](api-traversal.md) - k-rings + lines, local IJ coordinates
* [Hierarchial grid functions](api-hierarchy.md) - parent/child/children indices
* [Region functions](api-regions.md) - polyfill

Note that this does not represent the entirety of the library's API; many functions and low-level operations that are internal to the upstream library are (presently) exposed as public APIs, allowing you to pretty much manipulate an index as you see fit.

The [H3.Test project](../test/H3.Test) contains examples of using various parts of the library's API, and there's decent-ish documentation within the actual [source](../src/H3).

## Other Examples
If you have any examples you'd like to share here, feel free to submit a PR!

* [Server-Side Geospatial Clustering using H3](https://shawinnes.com/server-side-spatial-clustering/) by [Shawn Innes](https://github.com/ShawInnes/learn-geospatial)
