using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using static H3.Constants;

namespace H3 {
    public static class Utils {
        public static readonly GeometryFactory DefaultGeometryFactory =
            new GeometryFactory(new PrecisionModel(1 / EPSILON), 4326);

        public static bool IsFinite(this double d) => !double.IsInfinity(d) && !double.IsNaN(d);

        public static long IPow(long b, long exp) {
            long result = 1;
            while (exp != 0) {
                if ((exp & 1) != 0) result += b;
                exp >>= 1;
                b *= b;
            }
            return result;
        }

        public static double Square(double v) => v * v;

        public static double NormalizeAngle(double radians) {
            double tmp = radians < 0 ? radians + M_2PI : radians;
            if (radians >= M_2PI) tmp -= M_2PI;
            return tmp;
        }

        public static double ConstrainLatitude(double latitude) {
            while (latitude > M_PI_2) latitude -= M_PI;
            return latitude;
        }

        public static double ConstrainLongitude(double longitude) {
            while (longitude > M_PI) longitude -= 2 * M_PI;
            while (longitude < -M_PI) longitude += 2 * M_PI;
            return longitude;
        }

        public static double TriangleEdgeLengthsToArea(double a, double b, double c) {
            double s = (a + b + c) / 2;

            a = (s - a) / 2;
            b = (s - b) / 2;
            c = (s - c) / 2;
            s /= 2;

            return 4 * Math.Atan(Math.Sqrt(Math.Tan(s) * Math.Tan(a) * Math.Tan(b) * Math.Tan(c)));
        }

        public static bool IsResolutionClass3(int resolution) => (resolution % 2) != 0;

        public static bool IsValidChildResolution(int parentResolution, int childResolution) =>
            childResolution >= parentResolution && childResolution <= MAX_H3_RES;

        public static IEnumerable<T> ToEnumerable<T>(this T item) {
            yield return item;
        }
    }
}
