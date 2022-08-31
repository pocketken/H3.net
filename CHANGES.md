# pocketken.H3 Change Log

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
