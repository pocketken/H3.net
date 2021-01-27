using System;
using NetTopologySuite.Geometries;
using static H3.Constants;
using static H3.Utils;

#nullable enable

namespace H3.Model {

    public class Vec3d {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        #region lookups

        public static readonly Vec3d[] FaceCenters = new Vec3d[NUM_ICOSA_FACES] {
            new Vec3d(0.2199307791404606, 0.6583691780274996, 0.7198475378926182),     // face  0
            new Vec3d(-0.2139234834501421, 0.1478171829550703, 0.9656017935214205),    // face  1
            new Vec3d(0.1092625278784797, -0.4811951572873210, 0.8697775121287253),    // face  2
            new Vec3d(0.7428567301586791, -0.3593941678278028, 0.5648005936517033),    // face  3
            new Vec3d(0.8112534709140969, 0.3448953237639384, 0.4721387736413930),     // face  4
            new Vec3d(-0.1055498149613921, 0.9794457296411413, 0.1718874610009365),    // face  5
            new Vec3d(-0.8075407579970092, 0.1533552485898818, 0.5695261994882688),    // face  6
            new Vec3d(-0.2846148069787907, -0.8644080972654206, 0.4144792552473539),   // face  7
            new Vec3d(0.7405621473854482, -0.6673299564565524, -0.0789837646326737),   // face  8
            new Vec3d(0.8512303986474293, 0.4722343788582681, -0.2289137388687808),    // face  9
            new Vec3d(-0.7405621473854481, 0.6673299564565524, 0.0789837646326737),    // face 10
            new Vec3d(-0.8512303986474292, -0.4722343788582682, 0.2289137388687808),   // face 11
            new Vec3d(0.1055498149613919, -0.9794457296411413, -0.1718874610009365),   // face 12
            new Vec3d(0.8075407579970092, -0.1533552485898819, -0.5695261994882688),   // face 13
            new Vec3d(0.2846148069787908, 0.8644080972654204, -0.4144792552473539),    // face 14
            new Vec3d(-0.7428567301586791, 0.3593941678278027, -0.5648005936517033),   // face 15
            new Vec3d(-0.8112534709140971, -0.3448953237639382, -0.4721387736413930),  // face 16
            new Vec3d(-0.2199307791404607, -0.6583691780274996, -0.7198475378926182),  // face 17
            new Vec3d(0.2139234834501420, -0.1478171829550704, -0.9656017935214205),   // face 18
            new Vec3d(-0.1092625278784796, 0.4811951572873210, -0.8697775121287253),   // face 19
        };

        #endregion lookups

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

        public override bool Equals(object? other) => other is Vec3d v && X == v.X && Y == v.Y && Z == v.Z;

        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }

}
