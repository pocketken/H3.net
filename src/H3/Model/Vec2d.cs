using System;
using System.Runtime.CompilerServices;
using static H3.Constants;

#nullable enable

namespace H3.Model;

public struct Vec2d {

    public double X { get; set; }
    public double Y { get; set; }

    /// <summary>
    /// Returns the magnitude of the vector... ohhh yessss!  Un-pre-dictable!
    /// </summary>
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


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Intersect(Vec2d p0, Vec2d p1, Vec2d p2, Vec2d p3, ref Vec2d intersection) {
        Vec2d s1 = new(p1.X - p0.X, p1.Y - p0.Y);
        intersection.X = p3.X - p2.X;
        intersection.Y = p3.Y - p2.Y;
        var t = (intersection.X * (p0.Y - p2.Y) - intersection.Y * (p0.X - p2.X)) /
                (-intersection.X * s1.Y + s1.X * intersection.Y);

        intersection.X = p0.X + t * s1.X;
        intersection.Y = p0.Y + t * s1.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec2d Intersect(Vec2d p0, Vec2d p1, Vec2d p2, Vec2d p3, Vec2d? toUpdate = null) {
        var intersection = toUpdate ?? new Vec2d();
        Intersect(p0, p1, p2, p3, ref intersection);
        return intersection;
    }

    public LatLng ToFaceGeoCoord(int face, int resolution, bool isSubstrate) => FaceIJK.ToFaceGeoCoord(X, Y, face, resolution, isSubstrate);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vec2d a, Vec2d b) => Math.Abs(a.X - b.X) < FLT_EPSILON && Math.Abs(a.Y - b.Y) < FLT_EPSILON;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vec2d a, Vec2d b) => Math.Abs(a.X - b.X) >= FLT_EPSILON || Math.Abs(a.Y - b.Y) >= FLT_EPSILON;

    public override bool Equals(object? other) => other is Vec2d v && this == v;

    public override int GetHashCode() => HashCode.Combine(X, Y);
}