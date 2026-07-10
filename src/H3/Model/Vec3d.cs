using System;
using System.Runtime.CompilerServices;
using NetTopologySuite.Geometries;
using static H3.Constants;
using static H3.Utils;

#nullable enable

namespace H3.Model;

public struct Vec3d {

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double PointSquareDistance(Vec3d v2) =>
        Square(X - v2.X) + Square(Y - v2.Y) + Square(Z - v2.Z);

    public static Vec3d FromLatLng(LatLng coord) {
        return FromLonLat(coord.Longitude, coord.Latitude);
    }

    public static Vec3d FromLonLat(double longitudeRadians, double latitudeRadians) {
        unchecked {
            var r = Math.Cos(latitudeRadians);
            return new Vec3d(
                Math.Cos(longitudeRadians) * r,
                Math.Sin(longitudeRadians) * r,
                Math.Sin(latitudeRadians)
            );
        }
    }

    public static Vec3d FromPoint(Point point) => FromLatLng(LatLng.FromPoint(point));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vec3d a, Vec3d b) => Math.Abs(a.X - b.X) < EPSILON && Math.Abs(a.Y - b.Y) < EPSILON && Math.Abs(a.Z - b.Z) < EPSILON;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vec3d a, Vec3d b) => Math.Abs(a.X - b.X) >= EPSILON || Math.Abs(a.Y - b.Y) >= EPSILON || Math.Abs(a.Z - b.Z) >= EPSILON;

    public override bool Equals(object? other) => other is Vec3d v && this == v;

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

}