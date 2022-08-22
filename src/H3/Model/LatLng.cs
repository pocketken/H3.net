using System;
using H3.Extensions;
using NetTopologySuite.Geometries;
using static H3.Constants;
using static H3.Utils;

#nullable enable

namespace H3.Model;

[Obsolete("GeoCoord is now LatLng in H3 4.x+")]
public class GeoCoord : LatLng {

    public GeoCoord() { }

    public GeoCoord(double latitude, double longitude) {
        Latitude = latitude;
        Longitude = longitude;
    }

    public GeoCoord(GeoCoord source) {
        Latitude = source.Latitude;
        Longitude = source.Longitude;
    }

    public GeoCoord(LatLng source) {
        Latitude = source.Latitude;
        Longitude = source.Longitude;
    }
}

public class LatLng {

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double LatitudeDegrees => Latitude * M_180_PI;
    public double LongitudeDegrees => Longitude * M_180_PI;

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
    /// Return the NTS <see cref="Point"/> representation of this coordinate.
    /// </summary>
    /// <param name="geometryFactory"></param>
    /// <returns></returns>
    public Point ToPoint(GeometryFactory? geometryFactory = null) {
        var gf = geometryFactory ?? DefaultGeometryFactory;
        return gf.CreatePoint(new Coordinate(LongitudeDegrees, LatitudeDegrees));
    }

    /// <summary>
    /// Return the NTS <see cref="Coordinate"/> representation of this coordinate.
    /// </summary>
    /// <param name="retCoordinate">optional coordinate to update and return;
    /// defaults to allocating a new coordinate</param>
    /// <returns></returns>
    public Coordinate ToCoordinate(Coordinate? retCoordinate) {
        var coordinate = retCoordinate ?? new Coordinate();
        coordinate.X = LongitudeDegrees;
        coordinate.Y = LatitudeDegrees;
        return coordinate;
    }

    /// <summary>
    /// Return the NTS <see cref="Coordinate"/> representation of this coordinate.
    /// </summary>
    /// <returns></returns>
    public Coordinate ToCoordinate() {
        return ToCoordinate(new Coordinate());
    }

    /// <summary>
    /// Determines the azimuth to p2 from p1 in radians.
    /// </summary>
    /// <param name="p2">Destination spherical coordinate</param>
    /// <returns>The azimuth in radians from this to p2</returns>
    public double GetAzimuthInRadians(LatLng p2) {
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
    public double GetGreatCircleDistanceInRadians(LatLng p2) {
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
    public double GetPointDistanceInRadians(LatLng p2) {
        return GetGreatCircleDistanceInRadians(p2);
    }

    /// <summary>
    /// The great circle distance in kilometers between two spherical coordinates.
    /// </summary>
    /// <param name="p2">Destination coordinate</param>
    /// <returns>The great circle distance in kilometers between this coordinate
    /// and the destination coordinate.</returns>
    public double GetGreatCircleDistanceInKm(LatLng p2) {
        return GreatCircleDistanceInRadians(Longitude, Latitude, p2.Longitude, p2.Latitude) * EARTH_RADIUS_KM;
    }

    /// <summary>
    /// The great circle distance in kilometers between two spherical coordinates.
    /// </summary>
    /// <param name="p2">Destination coordinate</param>
    /// <returns>The great circle distance in kilometers between this coordinate
    /// and the destination coordinate.</returns>
    [Obsolete("as of 4.0: Use GetGreatCircleDistanceInKm instead")]
    public double GetPointDistanceInKm(LatLng p2) {
        return GetGreatCircleDistanceInKm(p2);
    }

    /// <summary>
    /// The great circle distance in meters between two spherical coordinates.
    /// </summary>
    /// <param name="p2">Destination coordinate</param>
    /// <returns>The great circle distance in meters between this coordinate
    /// and the destination coordinate.</returns>
    public double GetGreatCircleDistanceInMeters(LatLng p2) => GetGreatCircleDistanceInKm(p2) * 1000.0;

    /// <summary>
    /// The great circle distance in meters between two spherical coordinates.
    /// </summary>
    /// <param name="p2">Destination coordinate</param>
    /// <returns>The great circle distance in meters between this coordinate
    /// and the destination coordinate.</returns>
    [Obsolete("as of 4.0: Use GetGreatCircleDistanceInMeters instead")]
    public double GetPointDistanceInMeters(LatLng p2) => GetGreatCircleDistanceInMeters(p2);

    /// <summary>
    /// Returns an estimated number of cells that trace the cartesian-projected
    /// line
    /// </summary>
    /// <param name="other">Destination coordinates</param>
    /// <param name="resolution">H3 resolution used to trace the line</param>
    /// <returns>Estimated number of cells required to trace the line</returns>
    public int LineHexEstimate(LatLng other, int resolution) {
        // Get the area of the pentagon as the maximally-distorted area possible
        var firstPentagon = LookupTables.PentagonIndexesPerResolution[resolution][0];
        var pentagonRadiusKm = firstPentagon.GetRadiusInKm();
        var dist = GetGreatCircleDistanceInKm(other);
        var estimate = (int)Math.Ceiling(dist / (2 * pentagonRadiusKm));
        return estimate == 0 ? 1 : estimate;
    }

    public bool AlmostEqualsThreshold(LatLng p2, double threshold) =>
        Math.Abs(Latitude - p2.Latitude) < threshold && Math.Abs(Longitude - p2.Longitude) < threshold;

    public bool AlmostEquals(LatLng p2) => AlmostEqualsThreshold(p2, EPSILON_RAD);

    public static implicit operator LatLng((double, double) c) => new(c.Item1, c.Item2);

    public static bool operator ==(LatLng a, LatLng b) => Math.Abs(a.Latitude - b.Latitude) < EPSILON_RAD &&
                                                              Math.Abs(a.Longitude - b.Longitude) < EPSILON_RAD;

    public static bool operator !=(LatLng a, LatLng b) => Math.Abs(a.Latitude - b.Latitude) >= EPSILON_RAD ||
                                                              Math.Abs(a.Longitude - b.Longitude) >= EPSILON_RAD;

    public override bool Equals(object? other) {
        return other is LatLng c && this == c;
    }

    public override int GetHashCode() {
        return HashCode.Combine(Latitude, Longitude);
    }

}