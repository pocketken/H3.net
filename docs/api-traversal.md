# Grid traversal functions

## `GridDiskDistances` (`gridDiskDistances`)
Produces indices within `k` distance of the origin index.  Output may be returned in no particular order, and is an `IEnumerable<RingCell>` (simple holding struct that contains an `Index` and `Distance` from the origin).

```cs
var ring = index.GridDiskDistances(5);
// iterate, use .ToList() etc..
```

> **Implementation Notes**: This is implemented similarly to the upstream `gridDiskDistances` function in that it tries to use `GridDiskDistancesUnsafe(k).ToList()` (`gridDiskDistancesUnsafe`) first, and if that throws an exception due to pentagonal distortion it falls back to calling `GridDiskDistancesSafe(k)` (the non-recursive equivalent to upstream's `GridDiskDistancesSafe` method).  Depending on your input index, resolution, value of `k` and so on, you may find calling `GridDiskDistancesUnsafe` or `GridDiskDistancesSafe` directly ends up performing better and/or requires less memory than calling `GridDiskDistances` due to not having to try and materialize the `List`.  You may want to experiment and see what works best for your use case.

## `GridDiskDistancesSafe` (`gridDiskDistancesSafe`)
Produces indices within `k` distance of the origin index.  Output may be returned in no particular order, and is an `IEnumerable<RingCell>` (simple holding struct that contains an `Index` and `Distance` from the origin).

This is the "safe but slow" version of the k-ring (grid disk) algorithm implemented using a `Queue` instead of recursion.  This reduces stack depth for large values of `k` and and allow us to easily return an `IEnumerable<RingCell>`.

```cs
var ring = index.GridDiskDistancesSafe(5);
// iterate, use .ToList() etc..
```

## `GridDiskDistancesUnsafe` (`gridDiskDistancesUnsafe`)
Produces indices within `k` distance of the origin index.  Output may be returned in no particular order, and is an `IEnumerable<RingCell>` (simple holding struct that contains an `Index` and `Distance` from the origin).

This is the "unsafe" version of the k-ring (grid disk) algorithm that has "undefined behaviour" (throws an exception) if/when one of the indexes returned by this method is a pentagon or is in the pentagon distortion area.

```cs
var ring = index.GridDiskDistancesUnsafe(5);
// iterate, use .ToList() etc..
```

## `GridRingUnsafe` (`gridRingUnsafe`)
Produces the hollow hexagonal ring centered at origin with sides of length `k` (gives you all of the indexes for just the ring at distance `k`).  Returns `IEnumerable<H3Index>`.

Similar to `GridDiskDistancesUnsafe` this utilizes an "unsafe" version of the k-ring (grid disk) algorithm that has "undefined behaviour" (throws an exception) if/when one of the indexes returned by this method is a pentagon or is in the pentagon distortion area.

```cs
var hexRing = index.GridRingUnsafe(5);
// iterate, use .ToList() etc..
```

## `GridDistance` (`gridDistance`)
Returns the distance in grid cells between the two indexes.  Will return -1 if finding the distance failed (different resolutions, too far apart, separated by pentagonal distortion).

```cs
var distance = index.GridDistance(someOtherIndex);
```

## `GridPathCells` (`gridPathCells`)
Given two H3 indexes, returns the line of indexes between them (inclusive) as an `IEnumerable<H3Index>`.

As per the upstream library, there are some things to consider:

  * This function may fail to find the distance between two indexes, for example if they are very far apart or the indexes are on opposite sides of a pentagon
  * The specific output of this function should not be considered stable across library versions; the only guarantees are that the line length will be `GridDistance(start, end) + 1` and that every index in the line will be a neighbour of the preceding index
  * Lines are drawn in grid space, and may not correspond exactly to either Cartesian lines or great arcs.  You can always use the `Fill` polyfill method on a NTS `LineString` geometry if you need that sort of thing.

```cs
var line = originIndex.GridPathCells(destinationIndex);
// iterate, use .ToList() etc..
```

## `CellToLocalIj` (`cellToLocalIJ`)
Produces local IJ coordinates for an H3 index anchored by an origin.

Experiemental, output is not guaranteed, may eat your children etc..

```cs
Model.CoordIJ localIj = originIndex.CellToLocalIj(otherIndex);
```

## `LocalIjToCell` (`localIj`)
Produices an H3 index from local IJ coordinates anchored by an origin.

```cs
var otherIndex = origin.LocalIjToCell(localIj);
```
