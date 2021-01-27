using System;
using System.Collections.Generic;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using static H3.Constants;
using static H3.Utils;

#nullable enable

namespace H3.Model {

    public class FaceIJK {
        public int Face { get; set; }
        public CoordIJK Coord { get; set; } = new CoordIJK();

        public BaseCellRotation? BaseCellRotation {
            get {
                if (Coord.I > MAX_FACE_COORD || Coord.J > MAX_FACE_COORD || Coord.K > MAX_FACE_COORD) return null;

                try {
                    return BaseCellRotation.FaceIjkBaseCells[Face, Coord.I, Coord.J, Coord.K];
                } catch {
                    return null;
                }
            }
        }

        public BaseCell? BaseCell => BaseCellRotation?.BaseCell ?? null;

        public FaceIJK() { }

        public FaceIJK(FaceIJK other) {
            Face = other.Face;
            Coord = new CoordIJK(other.Coord);
        }

        public FaceIJK(int face, CoordIJK coord) {
            Face = face;
            Coord = coord;
        }

        public FaceIJK(int face, int i, int j, int k) {
            Face = face;
            Coord = new CoordIJK(i, j, k);
        }

        #region lookups

        public const int IJ = 1;
        public const int KI = 2;
        public const int JK = 3;

        // TODO do we need [,3] here?  looks like only [0] is used..?
        public static readonly double[,] AxisAzimuths = new double[NUM_ICOSA_FACES, 3] {
            { 5.619958268523939882, 3.525563166130744542, 1.431168063737548730 },  // face  0
            { 5.760339081714187279, 3.665943979320991689, 1.571548876927796127 },  // face  1
            { 0.780213654393430055, 4.969003859179821079, 2.874608756786625655 },  // face  2
            { 0.430469363979999913, 4.619259568766391033, 2.524864466373195467 },  // face  3
            { 6.130269123335111400, 4.035874020941915804, 1.941478918548720291 },  // face  4
            { 2.692877706530642877, 0.598482604137447119, 4.787272808923838195 },  // face  5
            { 2.982963003477243874, 0.888567901084048369, 5.077358105870439581 },  // face  6
            { 3.532912002790141181, 1.438516900396945656, 5.627307105183336758 },  // face  7
            { 3.494305004259568154, 1.399909901866372864, 5.588700106652763840 },  // face  8
            { 3.003214169499538391, 0.908819067106342928, 5.097609271892733906 },  // face  9
            { 5.930472956509811562, 3.836077854116615875, 1.741682751723420374 },  // face 10
            { 0.138378484090254847, 4.327168688876645809, 2.232773586483450311 },  // face 11
            { 0.448714947059150361, 4.637505151845541521, 2.543110049452346120 },  // face 12
            { 0.158629650112549365, 4.347419854898940135, 2.253024752505744869 },  // face 13
            { 5.891865957979238535, 3.797470855586042958, 1.703075753192847583 },  // face 14
            { 2.711123289609793325, 0.616728187216597771, 4.805518392002988683 },  // face 15
            { 3.294508837434268316, 1.200113735041072948, 5.388903939827463911 },  // face 16
            { 3.804819692245439833, 1.710424589852244509, 5.899214794638635174 },  // face 17
            { 3.664438879055192436, 1.570043776661997111, 5.758833981448388027 },  // face 18
            { 2.361378999196363184, 0.266983896803167583, 4.455774101589558636 },  // face 19
        };

        public static readonly int[,] AdjacentFaceDirections = new int[NUM_ICOSA_FACES, NUM_ICOSA_FACES] {
            {0,  KI, -1, -1, IJ, JK, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  // face 0
            {IJ, 0,  KI, -1, -1, -1, JK, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  // face 1
            {-1, IJ, 0,  KI, -1, -1, -1, JK, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  // face 2
            {-1, -1, IJ, 0,  KI, -1, -1, -1, JK, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  // face 3
            {KI, -1, -1, IJ, 0,  -1, -1, -1, -1, JK,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},  // face 4
            {JK, -1, -1, -1, -1, 0,  -1, -1, -1, -1,
             IJ, -1, -1, -1, KI, -1, -1, -1, -1, -1},  // face 5
            {-1, JK, -1, -1, -1, -1, 0,  -1, -1, -1,
             KI, IJ, -1, -1, -1, -1, -1, -1, -1, -1},  // face 6
            {-1, -1, JK, -1, -1, -1, -1, 0,  -1, -1,
             -1, KI, IJ, -1, -1, -1, -1, -1, -1, -1},  // face 7
            {-1, -1, -1, JK, -1, -1, -1, -1, 0,  -1,
             -1, -1, KI, IJ, -1, -1, -1, -1, -1, -1},  // face 8
            {-1, -1, -1, -1, JK, -1, -1, -1, -1, 0,
             -1, -1, -1, KI, IJ, -1, -1, -1, -1, -1},  // face 9
            {-1, -1, -1, -1, -1, IJ, KI, -1, -1, -1,
             0,  -1, -1, -1, -1, JK, -1, -1, -1, -1},  // face 10
            {-1, -1, -1, -1, -1, -1, IJ, KI, -1, -1,
             -1, 0,  -1, -1, -1, -1, JK, -1, -1, -1},  // face 11
            {-1, -1, -1, -1, -1, -1, -1, IJ, KI, -1,
             -1, -1, 0,  -1, -1, -1, -1, JK, -1, -1},  // face 12
            {-1, -1, -1, -1, -1, -1, -1, -1, IJ, KI,
             -1, -1, -1, 0,  -1, -1, -1, -1, JK, -1},  // face 13
            {-1, -1, -1, -1, -1, KI, -1, -1, -1, IJ,
             -1, -1, -1, -1, 0,  -1, -1, -1, -1, JK},  // face 14
            {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             JK, -1, -1, -1, -1, 0,  IJ, -1, -1, KI},  // face 15
            {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, JK, -1, -1, -1, KI, 0,  IJ, -1, -1},  // face 16
            {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, JK, -1, -1, -1, KI, 0,  IJ, -1},  // face 17
            {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, JK, -1, -1, -1, KI, 0,  IJ},  // face 18
            {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, JK, IJ, -1, -1, KI, 0}    // face 19
        };

        public static readonly int[] MaxDistanceByClass2Res = new int[] {
            2,        // res  0
            -1,       // res  1
            14,       // res  2
            -1,       // res  3
            98,       // res  4
            -1,       // res  5
            686,      // res  6
            -1,       // res  7
            4802,     // res  8
            -1,       // res  9
            33614,    // res 10
            -1,       // res 11
            235298,   // res 12
            -1,       // res 13
            1647086,  // res 14
            -1,       // res 15
            11529602  // res 16
        };

        public static readonly int[] UnitScaleByClass2Res = new int[] {
            1,       // res  0
            -1,      // res  1
            7,       // res  2
            -1,      // res  3
            49,      // res  4
            -1,      // res  5
            343,     // res  6
            -1,      // res  7
            2401,    // res  8
            -1,      // res  9
            16807,   // res 10
            -1,      // res 11
            117649,  // res 12
            -1,      // res 13
            823543,  // res 14
            -1,      // res 15
            5764801  // res 16
        };

        #endregion lookups

        public static FaceIJK FromGeoCoord(GeoCoord coord, int resolution) {
            Vec3d v3d = Vec3d.FromGeoCoord(coord);
            FaceIJK result = new();

            double sqd = v3d.PointSquareDistance(Vec3d.FaceCenters[0]);
            for (var f = 1; f < NUM_ICOSA_FACES; f += 1) {
                double sqdT = v3d.PointSquareDistance(Vec3d.FaceCenters[f]);
                if (sqdT < sqd) {
                    result.Face = f;
                    sqd = sqdT;
                }
            }

            double r = Math.Acos(1 - sqd / 2);
            Vec2d v = new();

            if (r >= EPSILON) {
                double theta = NormalizeAngle(AxisAzimuths[result.Face, 0] -
                    NormalizeAngle(GeoCoord.FaceCenters[result.Face].GetAzimuthInRadians(coord)));

                if (IsResolutionClass3(resolution)) theta = NormalizeAngle(theta - M_AP7_ROT_RADS);

                r = Math.Tan(r) / RES0_U_GNOMONIC;
                for (var i = 0; i < resolution; i += 1) r *= M_SQRT7;

                v.X = r * Math.Cos(theta);
                v.Y = r * Math.Sin(theta);
            }

            result.Coord = CoordIJK.FromVec2d(v);
            return result;
        }

        public GeoCoord ToGeoCoord(int resolution) => Coord.ToVec2d().ToFaceGeoCoord(Face, resolution, false);

        private FaceIJK[] GetVertices(CoordIJK[] class3Verts, CoordIJK[] class2Verts, ref int resolution) {
            var verts = IsResolutionClass3(resolution) ? class3Verts : class2Verts;
            var coord = CoordIJK.DownAperature3CounterClockwise(Coord);
            coord.DownAperature3Clockwise();

            if (IsResolutionClass3(resolution)) {
                coord.DownAperature7Clockwise();
                resolution += 1;
            }

            List<FaceIJK> result = new();
            for (var v = 0; v < verts.Length; v += 1) {
                result.Add(new FaceIJK(Face, (coord + verts[v]).Normalize()));
            }

            return result.ToArray();
        }

        public FaceIJK[] GetHexVertices(ref int resolution) =>
            GetVertices(CoordIJK.Class3HexVertices, CoordIJK.Class2HexVertices, ref resolution);

        public FaceIJK[] GetPentagonVertices(ref int resolution) =>
            GetVertices(CoordIJK.Class3PentagonVertices, CoordIJK.Class2PentagonVertices, ref resolution);

        public Overage AdjustOverageClass2(int resolution, bool pentagonLeading4, bool isSubstrate) {
            Overage overage = Overage.None;

            int maxDist = MaxDistanceByClass2Res[resolution];
            if (isSubstrate) maxDist *= 3;

            int sum = Coord.I + Coord.J + Coord.K;
            if (isSubstrate && sum == maxDist) {
                overage = Overage.FaceEdge;
            } else if (sum > maxDist) {
                overage = Overage.NewFace;

                FaceOrientIJK orientedFace;
                if (Coord.K > 0) {
                    if (Coord.J > 0) {
                        orientedFace = FaceOrientIJK.Neighbours[Face, JK];
                    } else {
                        orientedFace = FaceOrientIJK.Neighbours[Face, KI];

                        // adjust for the pentagonal missing sequence
                        if (pentagonLeading4) {
                            // translate origin to center of pentagon, rotate to adjust for the missing sequence
                            // and translate the origin back to the center of the triangle
                            Coord += (Coord - new CoordIJK(maxDist, 0, 0)).RotateClockwise();
                        }
                    }
                } else {
                    orientedFace = FaceOrientIJK.Neighbours[Face, IJ];
                }

                Face = orientedFace.Face;

                // rotate and translate for adjacent face
                for (int i = 0; i < orientedFace.CounterClockwiseRotations; i += 1) {
                    Coord.RotateCounterClockwise();
                }

                int unitScale = UnitScaleByClass2Res[resolution];
                if (isSubstrate) unitScale *= 3;
                Coord += orientedFace.Translate * unitScale;
                Coord.Normalize();

                // overage points on pentagon boundaries can end up on edges
                if (isSubstrate && Coord.I + Coord.J + Coord.K > maxDist) {
                    overage = Overage.FaceEdge;
                }
            }

            return overage;
        }

        public Overage AdjustPentagonVertexOverage(int resolution) {
            Overage overage;

            do {
                overage = AdjustOverageClass2(resolution, false, true);
            } while (overage == Overage.NewFace);

            return overage;
        }

        public Polygon GetBoundary(int resolution, int start, int length) {
            int adjustedResolution = resolution;
            FaceIJK[] verts = GetPentagonVertices(ref adjustedResolution);

            int additionalIteration = length == NUM_PENT_VERTS ? 1 : 0;

            int lastFace = -1;
            Overage lastOverage = Overage.None;
            List<Coordinate> coordinates = new();

            for (int vert = start; vert < start + length + additionalIteration; vert += 1) {
                int v = vert % NUM_PENT_VERTS;

                FaceIJK face = verts[v];
                Overage overage = face.AdjustOverageClass2(adjustedResolution, false, true);

                /*
                    Check for edge-crossing. Each face of the underlying icosahedron is a
                    different projection plane. So if an edge of the hexagon crosses an
                    icosahedron edge, an additional vertex must be introduced at that
                    intersection point. Then each half of the cell edge can be projected
                    to geographic coordinates using the appropriate icosahedron face
                    projection. Note that Class II cell edges have vertices on the face
                    edge, with no edge line intersections.
                */
                if (IsResolutionClass3(resolution) && vert > start && face.Face != lastFace && lastOverage != Overage.FaceEdge) {
                    // find hex2d of the two vertexes on original face
                    int lastV = (v + 5) % NUM_HEX_VERTS;
                    Vec2d orig2d0 = verts[lastV].Coord.ToVec2d();
                    Vec2d orig2d1 = verts[v].Coord.ToVec2d();

                    // find the appropriate icosa face edge vertexes
                    int maxDist = MaxDistanceByClass2Res[adjustedResolution];
                    Vec2d v0 = new Vec2d(3 * maxDist, 0);
                    Vec2d v1 = new Vec2d(-1.5 * maxDist, 3.0 * M_SQRT3_2 * maxDist);
                    Vec2d v2 = new Vec2d(-1.5 * maxDist, -3.0 * M_SQRT3_2 * maxDist);

                    Vec2d edge0;
                    Vec2d edge1;

                    int face2 = lastFace == Face ? face.Face : lastFace;

                    switch (AdjacentFaceDirections[Face, face2]) {
                        case IJ:
                            edge0 = v0;
                            edge1 = v1;
                            break;

                        case JK:
                            edge0 = v1;
                            edge1 = v2;
                            break;

                        case KI:
                            edge0 = v2;
                            edge1 = v0;
                            break;

                        default:
                            throw new Exception("Unsupported direction");
                    }

                    Vec2d intersection = Vec2d.Intersect(orig2d0, orig2d1, edge0, edge1);
                    bool atVertex = orig2d0 == intersection || orig2d1 == intersection;
                    if (!atVertex) {
                        Point point = intersection.ToFaceGeoCoord(Face, adjustedResolution, true).ToPoint();
                        coordinates.Add(new Coordinate(point.X, point.Y));
                    }
                }

                // convert vertex to lat/lon and add to the result
                // vert == start + NUM_HEX_VERTS is only used to test for possible
                // intersection on last edge
                if (vert < start + NUM_HEX_VERTS) {
                    Point point = face.Coord.ToVec2d().ToFaceGeoCoord(face.Face, adjustedResolution, true).ToPoint();
                    coordinates.Add(new Coordinate(point.X, point.Y));
                }

                lastFace = face.Face;
                lastOverage = overage;
            }

            return new Polygon(new LinearRing(coordinates.ToArray()));
        }

        public override bool Equals(object? other) => other is FaceIJK f && Face == f.Face && Coord == f.Coord;

        public override int GetHashCode() => HashCode.Combine(Face, Coord);
    }

}
