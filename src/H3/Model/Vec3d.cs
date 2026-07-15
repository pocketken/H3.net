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

    // --- libh3 v4.5.0 vec3d.h operations, transliterated verbatim so the inverse
    //     projection (hex2d -> geo) reproduces the reference bit-for-bit. ---

    /// <summary><c>a*v1 + b*v2</c>, componentwise (libh3 <c>vec3LinComb</c>).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3d LinComb(double a, Vec3d v1, double b, Vec3d v2) =>
        new(a * v1.X + b * v2.X, a * v1.Y + b * v2.Y, a * v1.Z + b * v2.Z);

    /// <summary>Cross product (libh3 <c>vec3Cross</c>).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3d Cross(Vec3d v1, Vec3d v2) =>
        new(v1.Y * v2.Z - v1.Z * v2.Y,
            v1.Z * v2.X - v1.X * v2.Z,
            v1.X * v2.Y - v1.Y * v2.X);

    /// <summary>Dot product (libh3 <c>vec3Dot</c>).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Dot(Vec3d v1, Vec3d v2) => v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;

    /// <summary>
    /// Unit-normalized copy (libh3 <c>vec3Normalize</c>).  A zero vector — whether a
    /// true zero or the result of the squared norm underflowing — maps to zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3d Normalize() {
        var norm = Math.Sqrt(X * X + Y * Y + Z * Z);
        var s = norm > 0.0 ? 1.0 / norm : 0.0;
        return new Vec3d(X * s, Y * s, Z * s);
    }

    /// <summary>Spherical coordinates of this vector (libh3 <c>vec3ToLatLng</c>): <c>asin(z), atan2(y, x)</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LatLng ToLatLng() => new(Math.Asin(Z), Math.Atan2(Y, X));

}