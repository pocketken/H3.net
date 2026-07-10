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
    public static LatLng ForAzimuthDistanceInRadians(LatLng p1, double azimuth, double distance) {
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
                // not due north or south
                var sinP1Lat = Math.Sin(p1.Latitude);
                var cosP1Lat = Math.Cos(p1.Latitude);
                var cosDist = Math.Cos(distance);
                var sinDist = Math.Sin(distance);
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
                    var cosP2Lat = Math.Cos(p2.Latitude);
#if NETSTANDARD2_0
                    var sinLon = Clamp(Math.Sin(az) * sinDist / cosP2Lat, -1.0, 1.0);
                    var cosLon = Clamp((cosDist - sinP1Lat * Math.Sin(p2.Latitude)) / cosP1Lat / cosP2Lat, -1.0, 1.0);
#else
                        var sinLon = Math.Clamp(Math.Sin(az) * sinDist / cosP2Lat, -1.0, 1.0);
                        var cosLon = Math.Clamp((cosDist - sinP1Lat * Math.Sin(p2.Latitude)) / cosP1Lat / cosP2Lat,
                            -1.0, 1.0);
#endif
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