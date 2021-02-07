using System;
using NetTopologySuite.Geometries;
using static H3.Constants;
using static H3.Utils;

#nullable enable

namespace H3.Model {
    public class GeoCoord {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public GeoCoord() { }

        public GeoCoord(double latitude, double longitude) {
            Latitude = latitude;
            Longitude = longitude;
        }

        public GeoCoord(GeoCoord source) {
            Latitude = source.Latitude;
            Longitude = source.Longitude;
        }

        public static GeoCoord FromPoint(Point p) => new GeoCoord {
            Latitude = p.Y * M_PI_180,
            Longitude = p.X * M_PI_180
        };

        public static GeoCoord ForAzimuthDistanceInRadians(GeoCoord p1, double azimuth, double distance) {
            GeoCoord p2 = new GeoCoord(p1);
            if (distance < EPSILON) return p2;

            double az = NormalizeAngle(azimuth);

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
                double sinP1Lat = Math.Sin(p1.Latitude);
                double cosP1Lat = Math.Cos(p1.Latitude);
                double cosDist = Math.Cos(distance);
                double sinDist = Math.Sin(distance);
                double sinLat = Math.Clamp(sinP1Lat * cosDist + cosP1Lat * sinDist * az, -1.0, 1.0);
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
                    double cosP2Lat = Math.Cos(p2.Latitude);
                    double sinLon = Math.Clamp(Math.Sin(az) * sinDist / cosP2Lat, -1.0, 1.0);
                    double cosLon = Math.Clamp((cosDist - sinP1Lat * Math.Sin(p2.Latitude)) / cosP1Lat / cosP2Lat, -1.0, 1.0);
                    p2.Longitude = ConstrainLongitude(p1.Longitude + Math.Atan2(sinLon, cosLon));
                }
            }

            return p2;
        }

        public static double GetTriangleArea(GeoCoord a, GeoCoord b, GeoCoord c) =>
            TriangleEdgeLengthsToArea(
                a.GetPointDistanceInRadians(b),
                b.GetPointDistanceInRadians(c),
                c.GetPointDistanceInRadians(a)
            );

        public Point ToPoint() => new Point(Longitude * M_180_PI, Latitude * M_180_PI);

        public double GetAzimuthInRadians(GeoCoord p2) {
            double cosP2Lat = Math.Cos(p2.Latitude);
            return Math.Atan2(
                cosP2Lat * Math.Sin(p2.Longitude - Longitude),
                Math.Cos(Latitude) * Math.Sin(p2.Latitude) -
                    Math.Sin(Latitude) * cosP2Lat * Math.Cos(p2.Longitude - Longitude)
            );
        }

        public double GetPointDistanceInRadians(GeoCoord p2) {
            double sinLat = Math.Sin((p2.Latitude - Latitude) / 2.0);
            double sinLon = Math.Sin((p2.Longitude - Longitude) / 2.0);
            double a = sinLat * sinLat * Math.Cos(Latitude) * Math.Cos(p2.Latitude) * sinLon * sinLon;
            return 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        public double GetPointDistanceInKm(GeoCoord p2) => GetPointDistanceInRadians(p2) * EARTH_RADIUS_KM;

        public double GetPointDistanceInMeters(GeoCoord p2) => GetPointDistanceInKm(p2) * 1000.0;

        public bool AlmostEqualsThreshold(GeoCoord p2, double threshold) =>
            Math.Abs(Latitude - p2.Latitude) < threshold && Math.Abs(Longitude - p2.Longitude) < threshold;

        public bool AlmostEquals(GeoCoord p2) => AlmostEqualsThreshold(p2, EPSILON_RAD);

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
