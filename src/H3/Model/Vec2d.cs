using System;

#nullable enable

namespace H3.Model {

    public sealed class Vec2d {

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

        public Vec2d((double, double) components) {
            X = components.Item1;
            Y = components.Item2;
        }

        public static Vec2d Intersect(Vec2d p0, Vec2d p1, Vec2d p2, Vec2d p3) {
            Vec2d s1 = new(p1.X - p0.X, p1.Y - p0.Y);
            Vec2d s2 = new(p3.X - p2.X, p3.Y - p2.Y);
            float t = (float)(s2.X * (p0.Y - p2.Y) - s2.Y * (p0.X - p2.X)) /
                (float)(-s2.X * s1.Y + s1.X * s2.Y);

            s2.X = p0.X + t * s1.X;
            s2.Y = p0.Y + t * s1.Y;

            return s2;
        }

        public GeoCoord ToFaceGeoCoord(int face, int resolution, bool isSubstrate) => FaceIJK.ToFaceGeoCoord(X, Y, face, resolution, isSubstrate);

        public static bool operator ==(Vec2d a, Vec2d b) => a.X == b.X && a.Y == b.Y;

        public static bool operator !=(Vec2d a, Vec2d b) => a.X != b.X || a.Y != b.Y;

        public override bool Equals(object? other) => other is Vec2d v && X == v.X && Y == v.Y;

        public override int GetHashCode() => HashCode.Combine(X, Y);
    }

}
