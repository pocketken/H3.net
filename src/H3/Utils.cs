using System;
#if NET5_0_OR_GREATER
using System.Numerics;
#endif
using System.Runtime.CompilerServices;
using NetTopologySuite.Geometries;
using static H3.Constants;

[assembly: InternalsVisibleTo("H3.Test")]
namespace H3; 

public static class Utils {

    public static readonly GeometryFactory DefaultGeometryFactory =
        new(new PrecisionModel(1 / EPSILON), 4326);

    /// <summary>
    /// Gets the specified number of top bits from the provided value.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="numBits"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetTopBits(this ulong value, int numBits) => value >> (64 - numBits);

#if NETSTANDARD2_0
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFinite(this double d) => !double.IsInfinity(d) && !double.IsNaN(d);
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Square(double v) => v * v;

    /// <summary>
    /// Determines the azimuth to p2 from p1 in radians.
    /// </summary>
    /// <param name="p1Lon">p1 longitude, in radians</param>
    /// <param name="p1Lat">p1 latitude, in radians</param>
    /// <param name="p2Lon">p2 longitude, in radians</param>
    /// <param name="p2Lat">p2 latitude, in radians</param>
    /// <returns>azimuth, ...in radians!</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double AzimuthInRadians(double p1Lon, double p1Lat, double p2Lon, double p2Lat) {
        var cosP2Lat = Math.Cos(p2Lat);
        return Math.Atan2(
            cosP2Lat * Math.Sin(p2Lon - p1Lon),
            Math.Cos(p1Lat) * Math.Sin(p2Lat) -
            Math.Sin(p1Lat) * cosP2Lat * Math.Cos(p2Lon - p1Lon)
        );
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double PointDistanceInRadians(double p1Lon, double p1Lat, double p2Lon, double p2Lat) {
        var sinLat = Math.Sin((p2Lat - p1Lat) / 2.0);
        var sinLon = Math.Sin((p2Lon - p1Lon) / 2.0);
        var a = sinLat * sinLat + Math.Cos(p1Lat) * Math.Cos(p2Lat) * sinLon * sinLon;
        return 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NormalizeAngle(double radians) {
        var tmp = radians < 0 ? radians + M_2PI : radians;
        if (radians >= M_2PI) tmp -= M_2PI;
        return tmp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ConstrainLongitude(double longitude) {
        while (longitude > M_PI) longitude -= 2 * M_PI;
        while (longitude < -M_PI) longitude += 2 * M_PI;
        return longitude;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double TriangleEdgeLengthsToArea(double a, double b, double c) {
        var s = (a + b + c) / 2;

        a = (s - a) / 2;
        b = (s - b) / 2;
        c = (s - c) / 2;
        s /= 2;

        return 4 * Math.Atan(Math.Sqrt(Math.Tan(s) * Math.Tan(a) * Math.Tan(b) * Math.Tan(c)));
    }

    /// <summary>
    /// Indicates whether or not the provided resolution has a Class 3 orientation.
    /// </summary>
    /// <param name="resolution"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsResolutionClass3(int resolution) => resolution % 2 != 0;

    /// <summary>
    /// Indicates whether or not the specified child resolution is valid relative to the
    /// provided parent resolution.
    /// </summary>
    /// <param name="parentResolution"></param>
    /// <param name="childResolution"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidChildResolution(int parentResolution, int childResolution) =>
        childResolution >= parentResolution && childResolution <= MAX_H3_RES;

#if NETSTANDARD2_0
    /// <summary>
    /// Clamps the specified value between <paramref name="min">min</paramref>
    /// and <paramref name="max">max</paramref>
    /// </summary>
    /// <param name="value">value to clamp</param>
    /// <param name="min">minimum value</param>
    /// <param name="max">maximum value</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Clamp<T>(T value, T min, T max) where T : IComparable<T> {
        var result = value;
        if (value.CompareTo(min) < 0) result = min;
        if (value.CompareTo(max) > 0) result = max;
        return result;
    }
#endif

    /// <summary>
    /// Round implementation which replicates the C away from zero behavior
    /// and for some reason performs better than Math.Round with midpoint rounding
    /// option.
    /// </summary>
    /// <remarks>See the "double version of round behaves as if implemented as follows"
    /// code here: https://en.cppreference.com/w/c/numeric/math/round#Notes
    /// </remarks>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CRound(double value) => IsNegative(value) ? Math.Ceiling(value - 0.5) : Math.Floor(value + 0.5);

    /// <summary>
    /// Determines if the specified value is negative.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsNegative(double value) =>
#if NETSTANDARD2_0
        BitConverter.DoubleToInt64Bits(value) < 0;
#else
        double.IsNegative(value);
#endif

#if !NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int LeadingZeros(int value) {
        if (value <= 0) return value == 0 ? 32 : 0;
        var n = 31;
        if (value >= 1 << 16) {
            n -= 16;
            value >>= 16;
        }
        if (value >= 1 << 8) {
            n -= 8;
            value >>= 8;
        }
        if (value >= 1 << 4) {
            n -= 4;
            value >>= 4;
        }
        if (value >= 1 << 2) {
            n -= 2;
            value >>= 2;
        }
        return n - (value >> 1);
    }
#endif

    /// <summary>
    /// Count the number of leading zero bits in a mask.
    /// Similar in behavior to the x86 instruction LZCNT.
    /// </summary>
    /// <param name="value">The value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LeadingZeros(this ulong value) {
#if NET5_0_OR_GREATER
        return BitOperations.LeadingZeroCount(value);
#else
        var x = (int)(value >> 32);
        return x == 0 ? 32 + LeadingZeros((int)(value >> 32)) : LeadingZeros(x);
#endif
    }

}