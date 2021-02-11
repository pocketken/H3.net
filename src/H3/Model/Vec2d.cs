using System;
using static H3.Constants;
using static H3.Utils;

#nullable enable

namespace H3.Model {

    public class Vec2d {
        public double X { get; set; }
        public double Y { get; set; }
        public double Magitude => Math.Sqrt(X * X + Y * Y);

        public Vec2d() { }

        public Vec2d(double x, double y) {
            X = x;
            Y = y;
        }

        public Vec2d(Vec2d source) {
            X = source.X;
            Y = source.Y;
        }

        public static Vec2d Intersect(Vec2d p0, Vec2d p1, Vec2d p2, Vec2d p3) {
            Vec2d s1 = new Vec2d(p1.X - p0.X, p1.Y - p0.Y);
            Vec2d s2 = new Vec2d(p3.X - p2.X, p3.Y - p2.Y);
            var t = (s2.X * (p0.Y - p2.Y) - s2.Y * (p0.X - p2.X)) /
                (-s2.X * s1.Y + s1.X * s2.Y);
            return new Vec2d(p0.X + (t * s1.X), p0.Y + (t * s1.Y));
        }

        public GeoCoord ToFaceGeoCoord(int face, int resolution, bool isSubstrate) {
            double r = Magitude;
            if (r < EPSILON) {
                return new GeoCoord(LookupTables.GeoFaceCenters[face]);
            }

            double theta = Math.Atan2(Y, X);

            for (var i = 0; i < resolution; i += 1) r /= M_SQRT7;
            if (isSubstrate) {
                r /= 3.0;
                if (IsResolutionClass3(resolution)) r /= M_SQRT7;
            }

            r = Math.Atan(r * RES0_U_GNOMONIC);
            if (!isSubstrate && IsResolutionClass3(resolution)) {
                theta = NormalizeAngle(theta + M_AP7_ROT_RADS);
            }

            theta = NormalizeAngle(LookupTables.AxisAzimuths[face, 0] - theta);
            return GeoCoord.ForAzimuthDistanceInRadians(LookupTables.GeoFaceCenters[face], theta, r);
        }

        public static bool operator ==(Vec2d a, Vec2d b) => a.X == b.X & a.Y == b.Y;

        public static bool operator !=(Vec2d a, Vec2d b) => a.X != b.X || a.Y != b.Y;

        public override bool Equals(object? other) => other is Vec2d v && X == v.X && Y == v.Y;

        public override int GetHashCode() => HashCode.Combine(X, Y);
    }

}
