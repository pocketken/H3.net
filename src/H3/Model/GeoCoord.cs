using System;
using H3.Extensions;
using NetTopologySuite.Geometries;
using static H3.Constants;
using static H3.Utils;

#nullable enable

namespace H3.Model {

    public class GeoCoord {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double LatitudeDegrees => Latitude * M_180_PI;
        public double LongitudeDegrees => Longitude * M_180_PI;

        public GeoCoord() { }

        public GeoCoord(double latitude, double longitude) {
            Latitude = latitude;
            Longitude = longitude;
        }

        public GeoCoord(GeoCoord source) {
            Latitude = source.Latitude;
            Longitude = source.Longitude;
        }

        /// <summary>
        /// Creates a GeoCoord from a NTS Point.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static GeoCoord FromPoint(Point p) => new() {
            Latitude = p.Y * M_PI_180,
            Longitude = p.X * M_PI_180
        };

        /// <summary>
        /// Creates a GeoCoord from a NTS Coordinate.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static GeoCoord FromCoordinate(Coordinate c) => new() {
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
        public static GeoCoord ForAzimuthDistanceInRadians(GeoCoord p1, double azimuth, double distance) {
            GeoCoord p2 = new(p1);
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
                var sinLat = Math.Clamp(sinP1Lat * cosDist + cosP1Lat * sinDist * Math.Cos(az), -1.0, 1.0);
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
                    var sinLon = Math.Clamp(Math.Sin(az) * sinDist / cosP2Lat, -1.0, 1.0);
                    var cosLon = Math.Clamp((cosDist - sinP1Lat * Math.Sin(p2.Latitude)) / cosP1Lat / cosP2Lat, -1.0, 1.0);
                    p2.Longitude = ConstrainLongitude(p1.Longitude + Math.Atan2(sinLon, cosLon));
                }
            }

            return p2;
        }

        /// <summary>
        /// Compute area in radians^2 of a spherical triangle, given its vertices.
        /// </summary>
        /// <param name="a">First triangle vertex</param>
        /// <param name="b">Second triangle vertex</param>
        /// <param name="c">Third triangle vertex</param>
        /// <returns>Area of triangle on unit sphere, in radians^2</returns>
        public static double GetTriangleArea(GeoCoord a, GeoCoord b, GeoCoord c) =>
            TriangleEdgeLengthsToArea(
                a.GetPointDistanceInRadians(b),
                b.GetPointDistanceInRadians(c),
                c.GetPointDistanceInRadians(a)
            );

        /// <summary>
        /// Return the NTS Point representation of this coordinate.
        /// </summary>
        /// <param name="geometryFactory"></param>
        /// <returns></returns>
        public Point ToPoint(GeometryFactory? geometryFactory = null) {
            var gf = geometryFactory ?? DefaultGeometryFactory;
            return gf.CreatePoint(new Coordinate(LongitudeDegrees, LatitudeDegrees));
        }

        /// <summary>
        /// Return the NTS Coordinate representation of this coordinate.
        /// </summary>
        /// <param name="geometryFactory"></param>
        /// <returns></returns>
        public Coordinate ToCoordinate() {
            return new Coordinate(LongitudeDegrees, LatitudeDegrees);
        }

        /// <summary>
        /// Determines the azimuth to p2 from p1 in radians.
        /// </summary>
        /// <param name="p2">Destination spherical coordinate</param>
        /// <returns>The azimuth in radians from this to p2</returns>
        public double GetAzimuthInRadians(GeoCoord p2) {
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
        public double GetPointDistanceInRadians(GeoCoord p2) {
            return PointDistanceInRadians(Longitude, Latitude, p2.Longitude, p2.Latitude);
        }

        /// <summary>
        /// The great circle distance in kilometers between two spherical coordinates.
        /// </summary>
        /// <param name="p2">Destination coordinate</param>
        /// <returns>The great circle distance in kilometers between this coordinate
        /// and the destination coordinate.</returns>
        public double GetPointDistanceInKm(GeoCoord p2) => GetPointDistanceInRadians(p2) * EARTH_RADIUS_KM;

        /// <summary>
        /// The great circle disance in meters between two spherical coordiantes.
        /// </summary>
        /// <param name="p2">Destination coordinate</param>
        /// <returns>The great circle distance in meters between this coordinate
        /// and the destination coordinate.</returns>
        public double GetPointDistanceInMeters(GeoCoord p2) => GetPointDistanceInKm(p2) * 1000.0;

        /// <summary>
        /// Returns an estimated number of cells that trace the cartesian-projected
        /// line
        /// </summary>
        /// <param name="other">Destination coordinates</param>
        /// <param name="resolution">H3 resolution used to trace the line</param>
        /// <returns>Estimated number of cells required to trace the line</returns>
        public int LineHexEstimate(GeoCoord other, int resolution) {
            // Get the area of the pentagon as the maximally-distorted area possible
            H3Index firstPentagon = LookupTables.PentagonIndexesPerResolution[resolution][0];
            var pentagonRadiusKm = firstPentagon.GetRadiusInKm();
            var dist = GetPointDistanceInKm(other);
            var estimate = (int)Math.Ceiling(dist / (2 * pentagonRadiusKm));
            return estimate == 0 ? 1 : estimate;
        }

        public bool AlmostEqualsThreshold(GeoCoord p2, double threshold) =>
            Math.Abs(Latitude - p2.Latitude) < threshold && Math.Abs(Longitude - p2.Longitude) < threshold;

        public bool AlmostEquals(GeoCoord p2) => AlmostEqualsThreshold(p2, EPSILON_RAD);

        public static implicit operator GeoCoord((double, double) c) => new(c.Item1, c.Item2);

        public static bool operator ==(GeoCoord a, GeoCoord b) => a.Latitude == b.Latitude && a.Longitude == b.Longitude;

        public static bool operator !=(GeoCoord a, GeoCoord b) => a.Latitude != b.Latitude || a.Longitude != b.Longitude;

        public override bool Equals(object? other) {
            return other is GeoCoord c && Latitude == c.Latitude && Longitude == c.Longitude;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Latitude, Longitude);
        }
    }

}
