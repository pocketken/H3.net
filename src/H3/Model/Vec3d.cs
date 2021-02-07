using System;
using NetTopologySuite.Geometries;
using static H3.Utils;

#nullable enable

namespace H3.Model {

    public class Vec3d {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Vec3d() { }

        public Vec3d(double x, double y, double z) {
            X = x;
            Y = y;
            Z = z;
        }

        public Vec3d(Vec3d source) {
            X = source.X;
            Y = source.Y;
            Z = source.Z;
        }

        public double PointSquareDistance(Vec3d v2) => Square(X - v2.X) + Square(Y - v2.Y) + Square(Z - v2.Z);

        public static Vec3d FromGeoCoord(GeoCoord coord) {
            double r = Math.Cos(coord.Latitude);
            return new Vec3d {
                Z = Math.Sin(coord.Latitude),
                X = Math.Cos(coord.Longitude) * r,
                Y = Math.Sin(coord.Longitude) * r
            };
        }

        public static Vec3d FromPoint(Point point) => FromGeoCoord(GeoCoord.FromPoint(point));

        public static bool operator ==(Vec3d a, Vec3d b) => a.X == b.X & a.Y == b.Y && a.Z == b.Z;

        public static bool operator !=(Vec3d a, Vec3d b) => a.X != b.X || a.Y != b.Y || a.Z != b.Z;

        public override bool Equals(object? other) => other is Vec3d v && X == v.X && Y == v.Y && Z == v.Z;

        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }

}
