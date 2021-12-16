# Indexing functions

## From/To Value Types

```cs
// from string (stringToH3)
var index = new H3Index("89283080dcbffff");

// to string (h3ToString)
var str = index.ToString();
```
```cs
// from ulong
var index = new H3Index(0x89283080dcbffff);

// operators allow for implicit casting
H3Index index = 0x89283080dcbffff;
ulong value = index;
```

## From/To Geospatial Coordinates

To create an index based upon a geospatial coordinate and a target H3 resolution (`geoToH3`):

```cs
// from lon/lat, with a resolution of 9 -- note NTS Coordinates are longitude (X) first,
// latitude (Y) second, unlike upstream/Model.GeoCoord which is latitude then longitude
// (AND in radians, not degrees).
var coordinate = new Coordinate(-122.40898669969357, 37.81331899988944);
var index = coordinate.ToH3Index(9);
```
```cs
// or if you have a Point
var index = H3Index.FromPoint(point, 9);
```

You can use `GeoCoord`s with radian inputs as well, which can be handy if you've got existing code using them you want to port over:

```cs
using H3.Model;

var index = H3Index.FromGeoCoord(new GeoCoord(0.659966917655, -2.1364398519396), 9);
```

To get the centroid of a given H3 index (`h3ToGeo`):

```cs
// gives you a NTS Coordinate
var coordinate = index.ToCoordinate();

// gives you a NTS Point
var point = index.ToPoint();

// gives you GeoCoord
var geoCoord = index.ToGeoCoord();
```

## `GetCellBoundary` (`h3ToGeoBoundary`)
Returns the `Polygon` cell boundary of a given H3 index.

```cs
// gives you a NTS Polygon
var polygon = index.GetCellBoundary();
```
