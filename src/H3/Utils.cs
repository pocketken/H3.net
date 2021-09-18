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

        public static bool IsFinite(this double d) => !double.IsInfinity(d) && !double.IsNaN(d);

        public static double Square(double v) => v * v;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NormalizeAngle(double radians) {
            double tmp = radians < 0 ? radians + M_2PI : radians;
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
            double s = (a + b + c) / 2;

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

    }
}
