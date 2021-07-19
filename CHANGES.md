# pocketken.H3 Change Log

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
