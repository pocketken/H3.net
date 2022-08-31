# Index inspection functions
The `H3Index` class provides a wrapper around an index value, and provides convenient computed fields/properties for various index attributes:

```cs
// getResolution
int resolution = index.Resolution;

// getBaseCellNumber
Nodel.BaseCell baseCell = index.BaseCell;

// isValidCell
bool isValid = index.IsValidCell;

// isResClassIII
bool isResClass3 = Utils.IsResolutionClass3(index.Resolution);

// isPentagon
bool isPentagon = index.IsPentagon;

// maxFaceCount
int maxFaces = index.MaximumFaceCount;
```

Note some of these properties are computed when you access them via bitwise manipulations of the index value.  So, while they aren't overly expensive, if you're repeatedly checking certain properties in a loop or somthing similar you may want to consider caching their values, e.g.:

```cs
// get the resolution once, instead of each loop iteration
var resolution = index.Resolution;
for (var r = 0; r <= resolution; r += 1>) {
    // ...
}
```

## `GetFaces` (`getIcosahedronFaces`)
Find all icosahedron faces intersected by a given index.  Faces are represented as integers from 0-19, inclusive.  The array is sparse, and empty (no intersection) array values are represented by -1.

```cs
var faces = index.GetFaces()
```
