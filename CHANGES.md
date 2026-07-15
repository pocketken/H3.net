# pocketken.H3 Change Log

### 4.5.0.1 - 2026-07-14

A performance and allocation pass on top of 4.5.0, plus new zero-allocation span APIs and a parallel polyfill.  No upstream H3 API changes; cell and index results remain identical to 4.5.0 (the projection fix below shifts geometry by a few ULP to match libh3 exactly).

##### Fixes :wrench:
- the spherical inverse projection (`ToLatLng`, `GetCellBoundary` and its vertices, directed-edge boundaries, and everything built on them) now matches upstream libh3 v4.5.0 bit-for-bit.  v4.5.0 replaced the spherical-law-of-cosines inverse with a 3D-vector construction (tangent basis + linear combination + normalize); the port now follows it in the same operation order.  Center and boundary coordinates shift by a few ULP from earlier 4.5.0 builds
- fixes a codegen regression on `net10.0` in `GetDirectNeighbour`/`GetDirectNeighbourWithoutRotations`: over-aggressive inlining of the generated per-digit rotation tables gave the method a large zero-initialized stack frame, making grid disk, grid ring, `IsNeighbour`, directed-edge and polyfill operations several times slower under the net10 JIT than under net8.  net10 is now at parity with net8 on these paths (net8 was unaffected)
- `Fill` now traces the polygon boundary in cells before flooding inward, matching upstream `polygonToCells` seeding.  Thin/narrow features, holes and antimeridian-crossing shapes that are contained but only reachable through non-contained neighbours (and so were previously dropped from a single interior seed) are now returned.  Locked to libh3's exact cell set by a differential corpus (thin sliver, concave L, box-with-hole, antimeridian quad, disjoint multipolygon)

##### Enhancements :tada:
- new zero-allocation span/buffer-fill overloads across the traversal, hierarchy and set APIs: `GridDisk`, `GridDiskDistances`, `GridRingUnsafe`, `GridPathCells`, `GetChildrenForResolution`, `CompactCells`, `UncompactCells` and `GetCellBoundaryVertices` gain overloads that fill a caller-owned `Span<T>` and return the number of cells written, with sizing helpers `MaxGridDiskSize`, `MaxGridRingSize`, `GridPathCellsSize`, `CellToChildrenSize` and `UncompactCellsSize`.  Allocation-free on a warm `ArrayPool`; the streaming `IEnumerable` APIs are unchanged and produce identical results
- new `ParallelFill` for large polygon fills: shards the polygon's envelope into horizontal strips, fills each concurrently and unions the result.  Produces the same (unordered) cell set as `Fill`.  It is opt-in and only pays off on large fills: it trades the sequential fill's flat allocation for wall-clock, and below a few thousand output cells the setup cost makes it slower than `Fill`

##### Performance :rocket:
- the forward and inverse spherical projections (`FromLatLng`/`ToLatLng`, cell boundaries, areas, edge lengths) drop a large amount of redundant transcendental work while staying bit-for-bit with the reference: precomputed per-face-center sin/cos and axis-azimuth tables, angle-subtraction identities in place of per-call `atan2`/`sin`/`cos`, a cancelled `cos(latitude)` in the longitude solve, a collapsed planar-radius `sqrt`, precomputed `M_SQRT7` powers, integer round-div-by-7 on the aperture-7 up-scaling chain, and per-vertex trig cached across the Cagnoli area loop
- aperture-7 digit-pair down-steps in the resolution walk are fused into a single normalization, and `ToFaceIJK` fuses the aperture-7 down-step with the neighbour walk; `GetDirectNeighbour` fast-paths the common no-base-cell-crossing tail and skips the identity rotation reorient
- polyfill flood fill replaces NTS's per-call allocating point-in-area locator with an allocation-free inline ray-crossing locator, replaces its `HashSet`/`Stack` working sets with pooled open-addressed structures presized from the geometry's area, and hoists per-cell invariants out of the neighbour walk
- grid disk, cell boundary and edge length drop iterator and intermediate-array allocations via eager span fills and `stackalloc` buffers; `CompactCells` uses a presized bucket and a primitive `ulong` sort, and `UncompactCells` uses a pooled open-addressed dedup table and an inlined child walk

##### Testing
- geometry parity is gated per operation against authoritative libh3 v4.5.0: per-cell outputs within 1 ULP and the whole-sphere cell-area sum within 3 ULP, with a persisted manifest recording the worst per-op ULP observed (index and cell outputs match exactly).  Added adversarial `Vec3d` edge-case coverage and per-op ULP diagnostics

### 4.5.0 - 2026-07-10

Aligns the port with upstream [H3 4.5.0](https://github.com/uber/h3/blob/master/CHANGELOG.md) and modernizes the supported .NET targets.

##### Breaking Changes :mega:
- retargets the library to `net10.0`, `net8.0`, `netstandard2.1` and `netstandard2.0`; EOL `net6.0`/`net7.0` targets have been dropped (`netstandard2.0` remains for Unity and .NET Framework consumers) [#98](https://github.com/pocketken/H3.net/issues/98), [#140](https://github.com/pocketken/H3.net/issues/140)
- `EdgeLengthRadians`/`EdgeLengthKilometers`/`EdgeLengthMeters` and `GetDirectedEdgeBoundaryVertices` now throw `ArgumentException` when the index is not a valid directed edge instead of silently returning 0 / an empty enumerable, matching upstream's `E_DIR_EDGE_INVALID` [#109](https://github.com/pocketken/H3.net/issues/109)
- `GridRing` is now pentagon safe, matching upstream 4.3.0+ `gridRing`: it no longer throws on pentagonal distortion, transparently falling back to a slower-but-correct traversal.  The previous throwing behaviour is available as `GridRingUnsafe` (= upstream `gridRingUnsafe`), which the obsolete `GetHexRing` now aliases.  `GridRing` also now validates its inputs
- `CellAreaInRadiansSquared` (and the Km²/m² variants) use upstream 4.5.0's Cagnoli formula with compensated summation; areas differ very slightly from previous releases (cell areas at a given resolution now sum to the area of the sphere to within 1e-14)
- `H3Index`, `CoordIJK`, `FaceIJK` and `LatLng` are now structs (completing `CoordIJ`, `Vec2d`, `Vec3d`, `FaceOrientIJK` from earlier work), eliminating per-index/per-vertex heap allocations throughout: `FromLatLng`/`FromPoint` and `ToLatLng`/`ToPoint` are measured allocation-free (previously 131 and 96 B/op), grid disk allocations drop ~2.3x and children iteration ~17x [#93](https://github.com/pocketken/H3.net/issues/93).  Value semantics apply: `null` is no longer a valid index (use `H3Index.Invalid`; `default(H3Index)` == `Invalid`), mutations affect only the local copy, and scratch-buffer/reuse parameters have been removed (`LocalCoordIJK.ToH3Index`, `CoordIJK.FromVec2d`/`CubeRound`, `ToFaceIJK`, `ToCoordinate`, `GetCellBoundaryVertices`, `Coordinate.ToH3Index`, `FaceIJK.FromLatLng`, `Vec3d.FromLonLat`); `H3Index.ToFaceWithInitializedFijk` takes the `FaceIJK` by `ref`
- the obsolete `GeoCoord` type is removed (structs cannot be subclassed) along with the obsolete members producing it (`H3Index.ToGeoCoord`/`FromGeoCoord`, `VertexToGeoCoord`, `GetUnidirectionalEdgeBoundaryVertices` — the latter two threw `InvalidCastException` at runtime anyway); `FaceIJK`/`Vec2d`/`Vec3d` members named `*GeoCoord*` are renamed to `*LatLng*`
- the `netstandard2.0`/`netstandard2.1` targets no longer depend on `System.Text.Json`, making the assembly consumable from Unity without the STJ dependency chain (this is what existing Unity-focused forks patch out) [#140](https://github.com/pocketken/H3.net/issues/140).  `H3IndexJsonConverter` (and the `[JsonConverter]` attribute on `H3Index`) are now only available on `net8.0`+, where STJ is part of the framework.  If you consume the netstandard targets from .NET Framework and rely on JSON serialization, copy the previous converter class into your project and register it via `JsonSerializerOptions.Converters`.  The (accidentally `public`) `IsExternalInit` shim is also no longer part of the netstandard assemblies (replaced by the internal, source-only Polyfill equivalent)
- internal lookup tables are now compile-time generated (single source of truth in the `H3.Data` source generator) and flattened, substantially reducing static initialization cost and steady-state heap usage.  This slims some (implementation-oriented) public surface: `BaseCells` is now a `static` class; the per-instance `BaseCell.NeighbouringCells`/`NeighbourRotations` arrays and `NeighbourDirections` dictionary are replaced by shared flattened tables (see the new `BaseCells.IsPentagonCellNumber(int)` helper); `LookupTables.NewDirectionClass2`/`NewDirectionClass3`/`NewAdjustmentClass2`/`NewAdjustmentClass3` and the `DirectionToUnitVector`/`UnitVectorToDirection` dictionaries are removed (the traversal data lives in generated packed tables; unit vectors remain available via `LookupTables.UnitVectors`); `LookupTables.PentagonIndexesPerResolution` is now a generated `H3Index[][]` instead of `Dictionary<int, H3Index[]>` (indexing by resolution is source compatible); and `LookupTables.FaceIjkBaseCells` is now a lazily-built property (the library itself uses the flat generated tables)
- `CompactCells` now produces deterministically ordered output (sorted by index value within each resolution, coarsest resolutions last) instead of hash-set ordering; as before, the only guarantee is that the result is a minimal covering set

##### Fixes :wrench:
- fixes `IsValidCell` rejecting valid cells at resolutions >= 11 (and accepting some invalid low-resolution indexes) due to 32-bit shift wraparound in its bit masks; validation is now a port of upstream's faster `isValidCell` [#111](https://github.com/pocketken/H3.net/issues/111)
- fixes `Vec2d.Intersect` to use `double` instead of `float`, improving the precision of geographic coordinate output ([upstream #652](https://github.com/uber/h3/pull/652)) [#86](https://github.com/pocketken/H3.net/issues/86)
- fixes possible overflows in `GetDirectNeighbour` rotations, `LocalIjToCell`/`LocalIjkToCell` coordinate conversions and `IsNeighbour` digit lookups ([upstream #706](https://github.com/uber/h3/pull/706), [#707](https://github.com/uber/h3/pull/707), [#677](https://github.com/uber/h3/pull/677), [#684](https://github.com/uber/h3/pull/684))
- `GridPathCells` is now bidirectional per upstream 4.5.0: paths that cross pentagon distortion relative to the origin's local coordinate chart are retried anchored at the destination ([upstream #1111](https://github.com/uber/h3/pull/1111))
- fixes a pattern-matching precedence bug in `GetDirectNeighbour`'s polar pentagon handling (`is not 118 or 8` dropped the base cell 8 check)
- updates the average hexagon edge-length/area lookup tables from stale 3.x values to the upstream 4.x recomputed values
- `GetCellBoundaries` now passes the caller's `GeometryFactory` through to the individual cell boundary polygons instead of only to the outer MultiPolygon
- fixes an off-by-one in the base cell range used to build `LookupTables.PentagonIndexesPerResolution`

##### Enhancements :tada:
- new hierarchy extensions `CellToChildPos`, `ChildPosToCell` and `CellToChildrenSize` (upstream 4.1.0) [#102](https://github.com/pocketken/H3.net/issues/102)
- new `H3Index.IsValidIndex` property validating any index mode (upstream 4.4.0's `isValidIndex`)
- new directed edge extensions `ReverseDirectedEdge` (upstream 4.4.0) and `DestinationToDirectedEdges`
- new `CellsToMultiPolygon` set extension producing the dissolved outline(s) of a cell set, with upstream 4.5.0 validation semantics (= `cellsToMultiPolygon`)
- new grid statistics on `H3Index`: `GetNumberOfCells`, `GetRes0Cells`, `GetPentagons`, `GetHexagonAreaAverageInKmSquared`/`InMSquared` and `GetHexagonEdgeLengthAverageInKm`/`InM`
- new `LatLng.GetLoopAreaInRadiansSquared` spherical loop area helper
- polyfill improvements [#53](https://github.com/pocketken/H3.net/issues/53): `Fill` now supports `Point`, `LineString`, `MultiPoint`, `MultiLineString` and (nested) `GeometryCollection` inputs; polygonal fills seed from every component, fixing fills of `MultiPolygon`s with disjoint components; and a new `Fill(geometry, resolution, Func<H3Index, bool>)` overload flood fills with a caller-provided containment predicate (e.g. for containment checks in a non-WGS84 CRS)
- canonical cell set operations [#61](https://github.com/pocketken/H3.net/issues/61): `CanonicalizeCells`, `IsCanonicalCells` and `CanonicalCellsContain` (binary-search point-in-coverage against possibly-compacted sets, without uncompacting)
- new component-based `H3Index.Create(resolution, baseCellNumber, digits)` with full validation (upstream 4.5.0 `constructCell`)
- performance: allocation-free `CoordIJK` -> `Direction` conversion (~10x faster; benefits `FromLatLng`/`FromPoint`), allocation-free `IsNeighbour` shared-parent fast path (~2.7x faster), presized traversal materialization, faster `IsValidCell`, and faster bitwise index rotations
- performance: static initialization allocates ~6x less (130 KB -> 22 KB; ~half the steady-state heap) via compile-time generated, flattened lookup tables; pentagon checks use a bitmask instead of object lookups; `GridDistance`/`CellToLocalIj` are ~2x faster and `GridPathCells` ~1.6x faster (flat base cell neighbour tables replace per-cell dictionaries); `CompactCells` is ~2x faster with ~2x fewer allocations (sorted sibling-run scan replaces nested dictionaries/sets); `GridDiskDistancesSafe` allocates ~2x less and is ~20% faster (array-based BFS with an open-addressed dedup table); `CellToVertexes` is ~1.4x faster (vertex rotations computed once per cell); polyfill flood fills allocate ~20% less and `LineHexEstimate` caches the per-resolution pentagon radius (large constant win for `TraceCoordinates`/`LineString` fills); plus smaller wins in `GetParentForResolution`, `GetCellBoundary` and direction rotations
- `NetTopologySuite` 2.6.0; source-only [Polyfill](https://github.com/SimonCropp/Polyfill) for netstandard backfills.  The netstandard targets now depend only on `NetTopologySuite` (plus `Microsoft.Bcl.HashCode` on `netstandard2.0`)
- XML documentation is now generated (and correctly named) for all targets
- pure struct members are annotated `readonly` (C# 8; no runtime/netstandard impact)
- new standalone `test/H3.CompareBenchmarks` suite comparing the published 4.0.0 package against the current tree (see its README; the two versions are not ABI compatible, so each is compiled separately)

Notes relative to upstream 4.5.0: `getIndexDigit` is covered by the existing `GetDirectionForResolution`; `describeH3Error` is not applicable (exceptions carry failure detail); buffer-sizing functions (`maxGridDiskSize` et al.) are unnecessary with lazy enumerables.

### 4.0.0.0 - 2022-08-30

##### Breaking Changes :mega:
- some terminology and naming changes to classes (e.g. `GeoCoord`), methods, etc. to hopefully better align with upstream 4.0.0.  Have tried to simplify upgrade w/`[Obsolete]` stubs, but, sorry in advance if anything breaks! [#64](https://github.com/pocketken/H3.net/issues/64)
- removes support for .NET 5 [#75](https://github.com/pocketken/H3.net/issues/74)
- fixes assembly naming -- the version is no longer included within the assembly name [#68](https://github.com/pocketken/H3.net/issues/68)

##### Enhancements :tada:
- minor IJK math optimizations [#78](https://github.com/pocketken/H3.net/pull/78)

### 3.7.2.1 - 2021-12-16

##### Breaking Changes :mega:

- Visibility on several classes + methods changed from `public` to `internal` in an effort to clean up the public-facing API.  Let me know if I've changed anything you're using
- `sealed` added to all classes

##### Enhancements :tada:

- Added multi-target support for `net6.0`, `net5.0`, `netstandard2.0` [#49](https://github.com/pocketken/H3.net/issues/49) and `netstandard2.1` [#46](https://github.com/pocketken/H3.net/issues/46)
- Implement polyfill modes [#45](https://github.com/pocketken/H3.net/issues/45)
- Reduced allocations / increased performance in `H3.Algorithms.Polyfill.Fill` and `H3.Algorithms.Lines.LineTo` [#42](https://github.com/pocketken/H3.net/issues/42)
- Other minor performance improvements and tweaks

### 3.7.2 - 2021-07-19

##### Breaking Changes :mega:

- Changed `Mode.Hexagon` to `Mode.Cell` to align with changes in upstream [#37](https://github.com/pocketken/H3.net/pull/37)

##### Enhancements :tada:

- Faster `H3Index.IsValid` based on [upstream PR 496](https://github.com/uber/h3/pull/496)

##### Fixes :wrench:

- implements bugfix for `h3NeighborRotations` (`H3HierarchyExtensions.GetDirectNeighbour`) from [upstream release 3.7.2](https://github.com/uber/h3/commit/fda03e297b1abe3ce8277794a7de8970601ed7c9)
- fixes JSON converter so that we don't swallow non-H3 hex strings when deserializing [#36](https://github.com/pocketken/H3.net/issues/36)

### 3.7.1.3 - 2021-04-22

##### Enhancements :tada:

- Minor performance optimizations, slight API adjustments (sorry!)  [#24](https://github.com/pocketken/H3.net/pull/24)

##### Fixes :wrench:

- Fixes issues serializing to JSON using `System.Text.Json`.  [#26](https://github.com/pocketken/H3.net/issues/26)

### 3.7.1.2 - 2021-03-26

##### Fixes :wrench:

- Fixes `DefaultGeometryFactory` to use EPSG 4326, not 4236.  [#22](https://github.com/pocketken/H3.net/issues/22)

### 3.7.1.1 - 2021-03-23

##### Fixes :wrench:

- Updates NTS dependency from NetTopologySuite.Core 1.x to NetTopologySuite 2.x.  [#20](https://github.com/pocketken/H3.net/issues/20)

### 3.7.1.0 - 2021-03-23

Initial nuget package release.
