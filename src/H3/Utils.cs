using System;
using System.Runtime.CompilerServices;
using NetTopologySuite.Geometries;
using static H3.Constants;

namespace H3 {

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(this double d) => !double.IsInfinity(d) && !double.IsNaN(d);

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
            T result = value;
            if (value.CompareTo(min) < 0) result = min;
            if (value.CompareTo(max) > 0) result = max;
            return result;
        }
        #endif

    }

}