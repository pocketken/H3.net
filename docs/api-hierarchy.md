# Hierarchial grid functions

## `GetParentForResolution` (`h3ToParent`)
Gets the parent (coarser) index at the specified resolution.
```cs
var index = new H3Index(0x89283080dcbffff);
var parentIndex = index.GetParentForResolution(5);
```

Note that you'll get back `H3Index.Invalid` (aka upstream's `H3_NULL`) if you specify an invalid parent resolution (<= the index resolution).

## `GetChildCenterForResolution` (`h3ToCenterChild`)
Gets the center child (finer) index at the specified resolution.

```cs
var index = new H3Index(0x89283080dcbffff);
var childIndex = index.GetChildCenterForResolution(12);
```

As with the `GetParentForResolution` method, you'll get `H3Index.Invalid` if your child resolution is invalid (>= the index resolution).

## `GetChildrenForResolution` (`h3ToChildren`)
Gets all of the child (finer) indexes at the specified resolution as an `IEnumerable<H3Index>`.

```cs
var index = new H3Index(0x89283080dcbffff);
var children = index.GetChildrenAtResolution(12);

// iterate, use .ToList() etc..
```

## `Compact` (`compact`)
Takes a set of cells and compacts them by removing duplicates and pruning full child branches to the parent level. This is also done for all parents recursively to get the minimum number of indexes that perfectly cover the defined space.  Returns a `List<H3Index>` (**not** an `IEnumerable<H3Index>`, so be weary of large numbers of input cells as we have to iterate / track them all as part of the compaction algorithm).

```cs
// given some enumerable set of indexes..
IEnumerable<H3Index> indexes = ...;
// .. return the compacted set
var compacted = indexes.Compact();
```

## `UncompactToResolution` (`uncompact`)
Takes a compacted set of cells and expands back to the original set of cells at a specific resolution.  Returns an `IEnumerable<H3Index>`.

```cs
// given a compacted set of indexes
var compactedIndexes = ...;
// .. get the uncompacted set at res 10
var uncompacted = compactedIndexes.UncompactToResolution(10);
```
