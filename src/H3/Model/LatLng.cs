using System;
using System.Collections.Generic;
using H3.Extensions;
using NetTopologySuite.Geometries;
using static H3.Constants;
using static H3.Utils;

#nullable enable

namespace H3.Model;

public struct LatLng : IEquatable<LatLng> {

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public readonly double LatitudeDegrees => Latitude * M_180_PI;
    public readonly double LongitudeDegrees => Longitude * M_180_PI;

    public LatLng() {
    }

    public LatLng(double latitude, double longitude) {
        Latitude = latitude;
        Longitude = longitude;
    }

    public LatLng(LatLng source) {
        Latitude = source.Latitude;
        Longitude = source.Longitude;
    }

    /// <summary>
    /// Creates a LatLng from a NTS Point.
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public static LatLng FromPoint(Point p) => new() {
        Latitude = p.Y * M_PI_180,
        Longitude = p.X * M_PI_180
    };

    /// <summary>
    /// Creates a LatLng from a NTS Coordinate.
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public static LatLng FromCoordinate(Coordinate c) => new() {
        Latitude = c.Y * M_PI_180,
        Longitude = c.X * M_PI_180
    };

    /// <summary>
    /// Computes the point on the sphere a specified azimuth and distance from
    /// another point.
    /// </summary>
    /// <param name="p1">The first spherical coordinate</param>
    /// <param name="azimuth">The desired azimuth from p1</param>
    /// <param name="distance">The desired distance from p1, must be non-negative.</param>
    /// <returns>
    /// The spherical coordinates at the desired azimuth and distance from p1
    /// </returns>
    public static LatLng ForAzimuthDistanceInRadians(LatLng p1, double azimuth, double distance) =>
        ForAzimuthDistanceInRadians(p1, azimuth, distance, Math.Sin(p1.Latitude), Math.Cos(p1.Latitude));

    /// <summary>
    /// Precomputed-latitude-trig overload of
    /// <see cref="ForAzimuthDistanceInRadians(LatLng,double,double)"/>: the caller
    /// supplies <paramref name="sinP1Lat"/> = <c>Math.Sin(p1.Latitude)</c> and
    /// <paramref name="cosP1Lat"/> = <c>Math.Cos(p1.Latitude)</c> so the spherical
    /// projection can reuse the (constant, per-face-center) values instead of
    /// recomputing them.  Bit-for-bit identical to the public overload.
    /// </summary>
    internal static LatLng ForAzimuthDistanceInRadians(LatLng p1, double azimuth, double distance, double sinP1Lat, double cosP1Lat) =>
        ForAzimuthDistanceInRadians(p1, azimuth, distance, sinP1Lat, cosP1Lat, Math.Sin(distance), Math.Cos(distance));

    /// <summary>
    /// Precomputed-distance-trig overload of
    /// <see cref="ForAzimuthDistanceInRadians(LatLng,double,double,double,double)"/>:
    /// the caller additionally supplies <paramref name="sinDist"/> =
    /// <c>Math.Sin(distance)</c> and <paramref name="cosDist"/> =
    /// <c>Math.Cos(distance)</c>.  Projection callers form the angular distance as
    /// <c>distance = Math.Atan(u)</c> from a gnomonic radius <c>u</c>, so they can
    /// obtain these from a single <c>sqrt</c> via the tangent identities
    /// <c>sin(atan u) = u / sqrt(1 + u^2)</c>, <c>cos(atan u) = 1 / sqrt(1 + u^2)</c>
    /// instead of paying for a sine and a cosine here.  Numerically within ~1 ULP of
    /// recomputing <c>Math.Sin(distance)</c> / <c>Math.Cos(distance)</c>.
    /// </summary>
    internal static LatLng ForAzimuthDistanceInRadians(LatLng p1, double azimuth, double distance, double sinP1Lat, double cosP1Lat, double sinDist, double cosDist) {
        unchecked {
            LatLng p2 = new(p1);
            if (distance < EPSILON) return p2;

            var az = NormalizeAngle(azimuth);

            if (az < EPSILON || Math.Abs(az - M_PI) < EPSILON) {
                // due north or south
                p2.Latitude = az < EPSILON ? p1.Latitude + distance : p1.Latitude - distance;

                if (Math.Abs(p2.Latitude - M_PI_2) < EPSILON) {
                    // north pole
                    p2.Latitude = M_PI_2;
                    p2.Longitude = 0;
                } else if (Math.Abs(p2.Latitude + M_PI_2) < EPSILON) {
                    // south pole
                    p2.Latitude = -M_PI_2;
                    p2.Longitude = 0;
                } else {
                    p2.Longitude = ConstrainLongitude(p1.Longitude);
                }
            } else {
                // not due north or south; sinP1Lat / cosP1Lat and sinDist / cosDist
                // supplied by caller
#if NETSTANDARD2_0
                var sinLat = Clamp(sinP1Lat * cosDist + cosP1Lat * sinDist * Math.Cos(az), -1.0, 1.0);
#else
                    var sinLat = Math.Clamp(sinP1Lat * cosDist + cosP1Lat * sinDist * Math.Cos(az), -1.0, 1.0);
#endif
                p2.Latitude = Math.Asin(sinLat);

                if (Math.Abs(p2.Latitude - M_PI_2) < EPSILON) {
                    // north pole
                    p2.Latitude = M_PI_2;
                    p2.Longitude = 0;
                } else if (Math.Abs(p2.Latitude + M_PI_2) < EPSILON) {
                    // south pole
                    p2.Latitude = -M_PI_2;
                    p2.Longitude = 0;
                } else {
                    // atan2 is invariant under a positive common scale on both of
                    // its arguments, so the cos(p2.Latitude) that scales sinLon and
                    // cosLon cancels; computing it (a cosine) and dividing by it
                    // twice is unnecessary, and the unscaled numerators stay within
                    // [-1, 1] so the defensive clamp is a no-op and is dropped.  The
                    // retained /cosP1Lat already exists, so no new division is added.
                    // sin(p2.Latitude) == sinLat exactly (p2.Latitude = Asin(sinLat)).
                    var sinLon = Math.Sin(az) * sinDist;
                    var cosLon = (cosDist - sinP1Lat * sinLat) / cosP1Lat;
                    p2.Longitude = ConstrainLongitude(p1.Longitude + Math.Atan2(sinLon, cosLon));
                }
            }

            return p2;
        }
    }

    /// <summary>
    /// Precomputed-azimuth-trig overload of
    /// <see cref="ForAzimuthDistanceInRadians(LatLng,double,double,double,double,double,double)"/>:
    /// the caller supplies <paramref name="sinAz"/> = <c>Math.Sin(azimuth)</c> and
    /// <paramref name="cosAz"/> = <c>Math.Cos(azimuth)</c> directly instead of the
    /// azimuth angle itself.  The inverse gnomonic projection forms the azimuth as
    /// <c>axisAzimuth - atan2(y, x)</c>, whose sine and cosine follow from the
    /// angle-subtraction identity applied to the (constant, per-face) axis-azimuth
    /// trig and the planar components — <c>cos(atan2(y,x)) = x / r</c>,
    /// <c>sin(atan2(y,x)) = y / r</c> — so it never needs the <c>atan2</c>, <c>cos</c>
    /// or <c>sin</c> that this method would otherwise compute.  Due-north/south is
    /// detected from <c>|sinAz| &lt; EPSILON</c>, with <c>cosAz</c>'s sign selecting the
    /// hemisphere (+ north, − south); this differs from the angle-based test only for
    /// measure-zero borderline inputs, which both branches map to the same point
    /// within <see cref="Constants.EPSILON_RAD"/>.  Numerically within ~1 ULP of the
    /// angle-based overload.
    /// </summary>
    internal static LatLng ForAzimuthDistanceInRadians(LatLng p1, double distance, double sinP1Lat, double cosP1Lat, double sinDist, double cosDist, double sinAz, double cosAz) {
        unchecked {
            LatLng p2 = new(p1);
            if (distance < EPSILON) return p2;

            if (Math.Abs(sinAz) < EPSILON) {
                // due north or south; cosAz sign picks the hemisphere
                p2.Latitude = cosAz >= 0.0 ? p1.Latitude + distance : p1.Latitude - distance;

                if (Math.Abs(p2.Latitude - M_PI_2) < EPSILON) {
                    // north pole
                    p2.Latitude = M_PI_2;
                    p2.Longitude = 0;
                } else if (Math.Abs(p2.Latitude + M_PI_2) < EPSILON) {
                    // south pole
                    p2.Latitude = -M_PI_2;
                    p2.Longitude = 0;
                } else {
                    p2.Longitude = ConstrainLongitude(p1.Longitude);
                }
            } else {
                // not due north or south; sin/cos of the azimuth and of the
                // face-center latitude / distance all supplied by the caller
#if NETSTANDARD2_0
                var sinLat = Clamp(sinP1Lat * cosDist + cosP1Lat * sinDist * cosAz, -1.0, 1.0);
#else
                var sinLat = Math.Clamp(sinP1Lat * cosDist + cosP1Lat * sinDist * cosAz, -1.0, 1.0);
#endif
                p2.Latitude = Math.Asin(sinLat);

                if (Math.Abs(p2.Latitude - M_PI_2) < EPSILON) {
                    // north pole
                    p2.Latitude = M_PI_2;
                    p2.Longitude = 0;
                } else if (Math.Abs(p2.Latitude + M_PI_2) < EPSILON) {
                    // south pole
                    p2.Latitude = -M_PI_2;
                    p2.Longitude = 0;
                } else {
                    // atan2 is invariant under a positive common scale on both of
                    // its arguments, so the cos(p2.Latitude) that scales sinLon and
                    // cosLon cancels; computing it (a cosine) and dividing by it
                    // twice is unnecessary, and the unscaled numerators stay within
                    // [-1, 1] so the defensive clamp is a no-op and is dropped.  The
                    // retained /cosP1Lat already exists, so no new division is added.
                    // sin(p2.Latitude) == sinLat exactly (p2.Latitude = Asin(sinLat)).
                    var sinLon = sinAz * sinDist;
                    var cosLon = (cosDist - sinP1Lat * sinLat) / cosP1Lat;
                    p2.Longitude = ConstrainLongitude(p1.Longitude + Math.Atan2(sinLon, cosLon));
                }
            }

            return p2;
        }
    }

    /// <summary>
    /// Compute area in radians^2 of a spherical triangle, given its vertices.
    /// </summary>
    /// <param name="a">First triangle vertex</param>
    /// <param name="b">Second triangle vertex</param>
    /// <param name="c">Third triangle vertex</param>
    /// <returns>Area of triangle on unit sphere, in radians^2</returns>
    public static double GetTriangleArea(LatLng a, LatLng b, LatLng c) =>
        TriangleEdgeLengthsToArea(
            a.GetGreatCircleDistanceInRadians(b),
            b.GetGreatCircleDistanceInRadians(c),
            c.GetGreatCircleDistanceInRadians(a)
        );

    /// <summary>
    /// Compute the spherical surface area of a closed polygon loop, in
    /// radians^2, given its vertices.  The loop is closed implicitly, i.e. the
    /// last vertex does not need to repeat the first.  The area is always in
    /// the range [0, 4 * pi].
    /// </summary>
    /// <param name="loop">Vertices of the loop</param>
    /// <returns>Area of the loop on the unit sphere, in radians^2</returns>
    public static double GetLoopAreaInRadiansSquared(IReadOnlyList<LatLng> loop) {
        var sum = 0.0;
        var compensation = 0.0;

        for (var i = 0; i < loop.Count; i += 1) {
            var j = (i + 1) % loop.Count;
            KahanAdd(ref sum, ref compensation, GetCagnoliAreaTerm(loop[i], loop[j]));
        }

        // the Cagnoli sum yields a signed area, with the sign switching with the
        // orientation of the vertices; normalize into [0, 4 * pi] by adding
        // 4 * pi when the signed area is negative
        if (sum < 0.0) {
            KahanAdd(ref sum, ref compensation, 4.0 * M_PI);
        }

        return sum;
    }

    /// <summary>
    /// <see cref="ReadOnlySpan{T}"/> overload of
    /// <see cref="GetLoopAreaInRadiansSquared(IReadOnlyList{LatLng})"/> that
    /// consumes the loop vertices straight out of a stack buffer, avoiding any
    /// heap materialization of the boundary.  Replicates the indexed algorithm
    /// exactly, so the result is bit-for-bit identical.
    ///
    /// Each Cagnoli edge term needs the sine and cosine of both endpoints'
    /// half-latitudes (<c>lat / 2 + pi / 4</c>).  Every boundary vertex is the
    /// <c>a</c>-endpoint of one edge and the <c>b</c>-endpoint of the adjacent
    /// edge, so <see cref="GetCagnoliAreaTerm"/> would evaluate each vertex's
    /// <see cref="Math.Sin"/> / <see cref="Math.Cos"/> twice.  This overload
    /// carries the previous vertex's trig into the next edge, computing each
    /// vertex's half-latitude sine/cosine exactly once — bit-for-bit identical
    /// (the deterministic <c>Sin</c>/<c>Cos</c> produce the same bits at either
    /// call site, and the per-edge product order, edge visitation order and thus
    /// the order-sensitive <see cref="KahanAdd"/> sequence are all unchanged),
    /// but with ~2 fewer transcendentals per edge.
    /// </summary>
    /// <param name="loop">Vertices of the loop; closed implicitly</param>
    /// <returns>Area of the loop on the unit sphere, in radians^2</returns>
    internal static double GetLoopAreaInRadiansSquared(ReadOnlySpan<LatLng> loop) {
        var n = loop.Length;
        if (n == 0) return 0.0;

        var sum = 0.0;
        var compensation = 0.0;

        // half-latitude sine/cosine of the first vertex, retained to close the
        // loop, and carried forward as the "previous" endpoint's trig
        var first = loop[0];
        var firstHalfLat = first.Latitude / 2.0 + M_PI / 4.0;
        var sinFirst = Math.Sin(firstHalfLat);
        var cosFirst = Math.Cos(firstHalfLat);

        var sinPrev = sinFirst;
        var cosPrev = cosFirst;
        var lonPrev = first.Longitude;

        for (var i = 1; i < n; i += 1) {
            var current = loop[i];
            var halfLat = current.Latitude / 2.0 + M_PI / 4.0;
            var sinCur = Math.Sin(halfLat);
            var cosCur = Math.Cos(halfLat);

            var sa = sinPrev * sinCur;
            var ca = cosPrev * cosCur;
            var d = current.Longitude - lonPrev;
            KahanAdd(ref sum, ref compensation, -2.0 * Math.Atan2(sa * Math.Sin(d), sa * Math.Cos(d) + ca));

            sinPrev = sinCur;
            cosPrev = cosCur;
            lonPrev = current.Longitude;
        }

        // closing edge from the last vertex back to the first, matching
        // term(loop[n - 1], loop[0]) of the indexed overload
        {
            var saC = sinPrev * sinFirst;
            var caC = cosPrev * cosFirst;
            var dC = first.Longitude - lonPrev;
            KahanAdd(ref sum, ref compensation, -2.0 * Math.Atan2(saC * Math.Sin(dC), saC * Math.Cos(dC) + caC));
        }

        // the Cagnoli sum yields a signed area, with the sign switching with the
        // orientation of the vertices; normalize into [0, 4 * pi] by adding
        // 4 * pi when the signed area is negative
        if (sum < 0.0) {
            KahanAdd(ref sum, ref compensation, 4.0 * M_PI);
        }

        return sum;
    }

    /// <summary>
    /// Streaming overload of <see cref="GetLoopAreaInRadiansSquared(IReadOnlyList{LatLng})"/>
    /// that consumes the loop vertices in a single forward pass, avoiding the
    /// need to materialize the boundary into an array.  Emits the exact same
    /// sequence of per-edge terms (in the same order) as the indexed overload,
    /// so the result is bit-for-bit identical.
    /// </summary>
    /// <param name="loop">Vertices of the loop; closed implicitly</param>
    /// <returns>Area of the loop on the unit sphere, in radians^2</returns>
    internal static double GetLoopAreaInRadiansSquared(IEnumerable<LatLng> loop) {
        var sum = 0.0;
        var compensation = 0.0;

        var first = default(LatLng);
        var previous = default(LatLng);
        var hasVertices = false;

        foreach (var vertex in loop) {
            if (!hasVertices) {
                first = vertex;
                hasVertices = true;
            } else {
                KahanAdd(ref sum, ref compensation, GetCagnoliAreaTerm(previous, vertex));
            }

            previous = vertex;
        }

        // close the loop with the final edge from the last vertex back to the
        // first, matching term(loop[n - 1], loop[0]) of the indexed overload
        if (hasVertices) {
            KahanAdd(ref sum, ref compensation, GetCagnoliAreaTerm(previous, first));
        }

        if (sum < 0.0) {
            KahanAdd(ref sum, ref compensation, 4.0 * M_PI);
        }

        return sum;
    }

    /// <summary>
    /// The per-edge term of the Cagnoli spherical area formula.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private static double GetCagnoliAreaTerm(LatLng a, LatLng b) {
        var aLat = a.Latitude / 2.0 + M_PI / 4.0;
        var bLat = b.Latitude / 2.0 + M_PI / 4.0;

        var sa = Math.Sin(aLat) * Math.Sin(bLat);
        var ca = Math.Cos(aLat) * Math.Cos(bLat);

        var d = b.Longitude - a.Longitude;
        return -2.0 * Math.Atan2(sa * Math.Sin(d), sa * Math.Cos(d) + ca);
    }

    /// <summary>
    /// Kahan compensated summation, improving the numerical accuracy of the
    /// sum of many small floating point terms.
    /// </summary>
    /// <param name="sum"></param>
    /// <param name="compensation"></param>
    /// <param name="value"></param>
    private static void KahanAdd(ref double sum, ref double compensation, double value) {
        var y = value - compensation;
        var t = sum + y;
        compensation = (t - sum) - y;
        sum = t;
    }

    /// <summary>
    /// Return the NTS <see cref="Point"/> representation of this coordinate.
    /// </summary>
    /// <param name="geometryFactory"></param>
    /// <returns></returns>
    public readonly Point ToPoint(GeometryFactory? geometryFactory = null) {
        var gf = geometryFactory ?? DefaultGeometryFactory;
        return gf.CreatePoint(new Coordinate(LongitudeDegrees, LatitudeDegrees));
    }

    /// <summary>
    /// Return the NTS <see cref="Coordinate"/> representation of this coordinate.
    /// </summary>
    /// <param name="retCoordinate">optional coordinate to update and return;
    /// defaults to allocating a new coordinate</param>
    /// <returns></returns>
    public readonly Coordinate ToCoordinate(Coordinate? retCoordinate) {
        var coordinate = retCoordinate ?? new Coordinate();
        coordinate.X = LongitudeDegrees;
        coordinate.Y = LatitudeDegrees;
        return coordinate;
    }

    /// <summary>
    /// Return the NTS <see cref="Coordinate"/> representation of this coordinate.
    /// </summary>
    /// <returns></returns>
    public readonly Coordinate ToCoordinate() {
        return ToCoordinate(new Coordinate());
    }

    /// <summary>
    /// Determines the azimuth to p2 from p1 in radians.
    /// </summary>
    /// <param name="p2">Destination spherical coordinate</param>
    /// <returns>The azimuth in radians from this to p2</returns>
    public readonly double GetAzimuthInRadians(LatLng p2) {
        return AzimuthInRadians(Longitude, Latitude, p2.Longitude, p2.Latitude);
    }

    /// <summary>
    /// The great circle distance in radians between two spherical coordinates.
    ///
    /// This function uses the Haversine formula.
    /// For math details, see:
    ///  * https://en.wikipedia.org/wiki/Haversine_formula
    ///  * https://www.movable-type.co.uk/scripts/latlong.html
    /// </summary>
    /// <param name="p2">Destination coordinate</param>
    /// <returns>The great circle distance in radians between this coordinate
    /// and the destination coordinate.</returns>
    public readonly double GetGreatCircleDistanceInRadians(LatLng p2) {
        return GreatCircleDistanceInRadians(Longitude, Latitude, p2.Longitude, p2.Latitude);
    }

    /// <summary>
    /// The great circle distance in radians between two spherical coordinates.
    ///
    /// This function uses the Haversine formula.
    /// For math details, see:
    ///  * https://en.wikipedia.org/wiki/Haversine_formula
    ///  * https://www.movable-type.co.uk/scripts/latlong.html
    /// </summary>
    /// <param name="p2">Destination coordinate</param>
    /// <returns>The great circle distance in radians between this coordinate
    /// and the destination coordinate.</returns>
    [Obsolete("as of 4.0: Use GetGreatCircleDistanceInRadians instead")]
    public readonly double GetPointDistanceInRadians(LatLng p2) {
        return GetGreatCircleDistanceInRadians(p2);
    }

    /// <summary>
    /// The great circle distance in kilometers between two spherical coordinates.
    /// </summary>
    /// <param name="p2">Destination coordinate</param>
    /// <returns>The great circle distance in kilometers between this coordinate
    /// and the destination coordinate.</returns>
    public readonly double GetGreatCircleDistanceInKm(LatLng p2) {
        return GreatCircleDistanceInRadians(Longitude, Latitude, p2.Longitude, p2.Latitude) * EARTH_RADIUS_KM;
    }

    /// <summary>
    /// The great circle distance in kilometers between two spherical coordinates.
    /// </summary>
    /// <param name="p2">Destination coordinate</param>
    /// <returns>The great circle distance in kilometers between this coordinate
    /// and the destination coordinate.</returns>
    [Obsolete("as of 4.0: Use GetGreatCircleDistanceInKm instead")]
    public readonly double GetPointDistanceInKm(LatLng p2) {
        return GetGreatCircleDistanceInKm(p2);
    }

    /// <summary>
    /// The great circle distance in meters between two spherical coordinates.
    /// </summary>
    /// <param name="p2">Destination coordinate</param>
    /// <returns>The great circle distance in meters between this coordinate
    /// and the destination coordinate.</returns>
    public readonly double GetGreatCircleDistanceInMeters(LatLng p2) => GetGreatCircleDistanceInKm(p2) * 1000.0;

    /// <summary>
    /// The great circle distance in meters between two spherical coordinates.
    /// </summary>
    /// <param name="p2">Destination coordinate</param>
    /// <returns>The great circle distance in meters between this coordinate
    /// and the destination coordinate.</returns>
    [Obsolete("as of 4.0: Use GetGreatCircleDistanceInMeters instead")]
    public readonly double GetPointDistanceInMeters(LatLng p2) => GetGreatCircleDistanceInMeters(p2);

    /// <summary>
    /// Returns an estimated number of cells that trace the cartesian-projected
    /// line
    /// </summary>
    /// <param name="other">Destination coordinates</param>
    /// <param name="resolution">H3 resolution used to trace the line</param>
    /// <returns>Estimated number of cells required to trace the line</returns>
    public readonly int LineHexEstimate(LatLng other, int resolution) {
        // Get the area of the pentagon as the maximally-distorted area possible
        var pentagonRadiusKm = PentagonRadiusKmPerResolution[resolution];
        var dist = GetGreatCircleDistanceInKm(other);
        var estimate = (int)Math.Ceiling(dist / (2 * pentagonRadiusKm));
        return estimate == 0 ? 1 : estimate;
    }

    private static double[]? _pentagonRadiusKmPerResolution;

    /// <summary>
    /// The radius of the first pentagon at each resolution, in km; pentagons
    /// are the maximally-distorted cells.
    /// </summary>
    private static double[] PentagonRadiusKmPerResolution {
        get {
            var cache = _pentagonRadiusKmPerResolution;
            if (cache != null) return cache;

            cache = new double[MAX_H3_RES + 1];
            for (var resolution = 0; resolution <= MAX_H3_RES; resolution += 1) {
                cache[resolution] = LookupTables.PentagonIndexesPerResolution[resolution][0].GetRadiusInKm();
            }

            _pentagonRadiusKmPerResolution = cache;
            return cache;
        }
    }

    public readonly bool AlmostEqualsThreshold(LatLng p2, double threshold) =>
        Math.Abs(Latitude - p2.Latitude) < threshold && Math.Abs(Longitude - p2.Longitude) < threshold;

    public readonly bool AlmostEquals(LatLng p2) => AlmostEqualsThreshold(p2, EPSILON_RAD);

    public static implicit operator LatLng((double, double) c) => new(c.Item1, c.Item2);

    public static bool operator ==(LatLng a, LatLng b) => Math.Abs(a.Latitude - b.Latitude) < EPSILON_RAD &&
                                                              Math.Abs(a.Longitude - b.Longitude) < EPSILON_RAD;

    public static bool operator !=(LatLng a, LatLng b) => Math.Abs(a.Latitude - b.Latitude) >= EPSILON_RAD ||
                                                              Math.Abs(a.Longitude - b.Longitude) >= EPSILON_RAD;

    public readonly bool Equals(LatLng other) => this == other;

    public override readonly bool Equals(object? other) {
        return other is LatLng c && this == c;
    }

    public override readonly int GetHashCode() {
        return HashCode.Combine(Latitude, Longitude);
    }

}