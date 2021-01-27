using System;
using NetTopologySuite.Geometries;
using static H3.Constants;
using static H3.Utils;

#nullable enable

namespace H3.Model {
    public class GeoCoord {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        #region lookups

        public static readonly GeoCoord[] FaceCenters = new[] {
            new GeoCoord(0.803582649718989942, 1.248397419617396099),    // face  0
            new GeoCoord(1.307747883455638156, 2.536945009877921159),    // face  1
            new GeoCoord(1.054751253523952054, -1.347517358900396623),   // face  2
            new GeoCoord(0.600191595538186799, -0.450603909469755746),   // face  3
            new GeoCoord(0.491715428198773866, 0.401988202911306943),    // face  4
            new GeoCoord(0.172745327415618701, 1.678146885280433686),    // face  5
            new GeoCoord(0.605929321571350690, 2.953923329812411617),    // face  6
            new GeoCoord(0.427370518328979641, -1.888876200336285401),   // face  7
            new GeoCoord(-0.079066118549212831, -0.733429513380867741),  // face  8
            new GeoCoord(-0.230961644455383637, 0.506495587332349035),   // face  9
            new GeoCoord(0.079066118549212831, 2.408163140208925497),    // face 10
            new GeoCoord(0.230961644455383637, -2.635097066257444203),   // face 11
            new GeoCoord(-0.172745327415618701, -1.463445768309359553),  // face 12
            new GeoCoord(-0.605929321571350690, -0.187669323777381622),  // face 13
            new GeoCoord(-0.427370518328979641, 1.252716453253507838),   // face 14
            new GeoCoord(-0.600191595538186799, 2.690988744120037492),   // face 15
            new GeoCoord(-0.491715428198773866, -2.739604450678486295),  // face 16
            new GeoCoord(-0.803582649718989942, -1.893195233972397139),  // face 17
            new GeoCoord(-1.307747883455638156, -0.604647643711872080),  // face 18
            new GeoCoord(-1.054751253523952054, 1.794075294689396615),   // face 19
        };

        #endregion lookups

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

        public override bool Equals(object? other) {
            return other is GeoCoord c && Latitude == c.Latitude && Longitude == c.Longitude;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Latitude, Longitude);
        }
    }

}
