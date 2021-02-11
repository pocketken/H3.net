using System;
using System.Collections.Generic;
using static H3.Constants;
using static H3.Utils;

#nullable enable

namespace H3.Model {

    public class FaceIJK {
        public int Face { get; set; }
        public CoordIJK Coord { get; set; } = new CoordIJK();

        public const int IJ = 1;
        public const int KI = 2;
        public const int JK = 3;

        public BaseCellRotation? BaseCellRotation {
            get {
                if (Coord.I > MAX_FACE_COORD || Coord.J > MAX_FACE_COORD || Coord.K > MAX_FACE_COORD) return null;

                try {
                    return LookupTables.FaceIjkBaseCells[Face, Coord.I, Coord.J, Coord.K];
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

        public static FaceIJK FromGeoCoord(GeoCoord coord, int resolution) {
            Vec3d v3d = Vec3d.FromGeoCoord(coord);
            FaceIJK result = new();

            double sqd = v3d.PointSquareDistance(LookupTables.FaceCenters[0]);
            for (var f = 1; f < NUM_ICOSA_FACES; f += 1) {
                double sqdT = v3d.PointSquareDistance(LookupTables.FaceCenters[f]);
                if (sqdT < sqd) {
                    result.Face = f;
                    sqd = sqdT;
                }
            }

            double r = Math.Acos(1 - sqd / 2);
            Vec2d v = new();

            if (r >= EPSILON) {
                double theta = NormalizeAngle(LookupTables.AxisAzimuths[result.Face, 0] -
                    NormalizeAngle(LookupTables.GeoFaceCenters[result.Face].GetAzimuthInRadians(coord)));

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

            // if res is Class III we need to add a cw aperture 7 to get to
            // icosahedral Class II
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

        /// <summary>
        /// Get the vertices of a cell as substrate FaceIJK addresses.
        /// </summary>
        /// <param name="resolution">The H3 resolution of the cell. This may be adjusted if
        /// necessary for the substrate grid resolution.</param>
        /// <returns>cell vertices</returns>
        public FaceIJK[] GetHexVertices(ref int resolution) =>
            GetVertices(LookupTables.Class3HexVertices, LookupTables.Class2HexVertices, ref resolution);

        /// <summary>
        /// Get the vertices of a pentagon cell as substrate FaceIJK addresses.
        /// </summary>
        /// <param name="resolution">The H3 resolution of the cell. This may be adjusted if
        /// necessary for the substrate grid resolution.</param>
        /// <returns>cell vertices</returns>
        public FaceIJK[] GetPentagonVertices(ref int resolution) =>
            GetVertices(LookupTables.Class3PentagonVertices, LookupTables.Class2PentagonVertices, ref resolution);

        /// <summary>
        /// Adjusts a FaceIJK address in place so that the resulting cell address is
        /// relative to the correct icosahedral face.
        /// </summary>
        /// <param name="resolution">H3 resolution of the cell</param>
        /// <param name="pentagonLeading4">Whether or not the cell is a pentagon with
        /// leading digit of 4 (Direction.I)</param>
        /// <param name="isSubstrate">Whether or not the cell is on a substrate grid</param>
        /// <returns></returns>
        public Overage AdjustOverageClass2(int resolution, bool pentagonLeading4, bool isSubstrate) {
            Overage overage = Overage.None;

            int maxDist = LookupTables.MaxDistanceByClass2Res[resolution];
            if (isSubstrate) maxDist *= 3;

            int sum = Coord.I + Coord.J + Coord.K;
            if (isSubstrate && sum == maxDist) {
                overage = Overage.FaceEdge;
            } else if (sum > maxDist) {
                overage = Overage.NewFace;

                FaceOrientIJK orientedFace;
                if (Coord.K > 0) {
                    if (Coord.J > 0) {
                        orientedFace = LookupTables.OrientedFaceNeighbours[Face, JK];
                    } else {
                        orientedFace = LookupTables.OrientedFaceNeighbours[Face, KI];

                        // adjust for the pentagonal missing sequence
                        if (pentagonLeading4) {
                            // translate origin to center of pentagon, rotate to adjust for the missing sequence
                            // and translate the origin back to the center of the triangle
                            CoordIJK origin = new CoordIJK(maxDist, 0, 0);
                            Coord = (Coord - origin).RotateClockwise() + origin;
                        }
                    }
                } else {
                    orientedFace = LookupTables.OrientedFaceNeighbours[Face, IJ];
                }

                Face = orientedFace.Face;

                // rotate and translate for adjacent face
                for (int i = 0; i < orientedFace.CounterClockwiseRotations; i += 1) {
                    Coord.RotateCounterClockwise();
                }

                int unitScale = LookupTables.UnitScaleByClass2Res[resolution];
                if (isSubstrate) unitScale *= 3;
                Coord += orientedFace.Translate * unitScale;
                Coord.Normalize();

                // overage points on pentagon boundaries can end up on edges
                if (isSubstrate && ((Coord.I + Coord.J + Coord.K) == maxDist)) {
                    overage = Overage.FaceEdge;
                }
            }

            return overage;
        }

        /// <summary>
        /// Adjusts a FaceIJK address for a pentagon vertex in a substrate grid in
        /// place so that the resulting cell address is relative to the correct
        /// icosahedral face.
        /// </summary>
        /// <param name="resolution">H3 resolution of the cell</param>
        /// <returns></returns>
        public Overage AdjustPentagonVertexOverage(int resolution) {
            Overage overage;

            do {
                overage = AdjustOverageClass2(resolution, false, true);
            } while (overage == Overage.NewFace);

            return overage;
        }

        /// <summary>
        /// Generates the cell boundary in spherical coordinates for a pentagonal cell
        /// given by a FaceIJK address at a specified resolution.
        /// </summary>
        /// <param name="resolution">The H3 resolution of the cell</param>
        /// <param name="start">The first topological vertex to return</param>
        /// <param name="length">The number of topological vertexes to return</param>
        /// <returns>The spherical coordinates of the cell boundary</returns>
        public IEnumerable<GeoCoord> GetPentagonBoundary(int resolution, int start, int length) {
            int adjustedResolution = resolution;
            FaceIJK[] verts = GetPentagonVertices(ref adjustedResolution);

            // If we're returning the entire loop, we need one more iteration in case
            // of a distortion vertex on the last edge
            int additionalIteration = length == NUM_PENT_VERTS ? 1 : 0;

            // convert each vertex to lat/lon
            // adjust the face of each vertex as appropriate and introduce
            // edge-crossing vertices as needed
            FaceIJK lastFijk = new FaceIJK();

            for (int vert = start; vert < start + length + additionalIteration; vert += 1) {
                int v = vert % NUM_PENT_VERTS;

                FaceIJK fijk = new FaceIJK(verts[v]);
                fijk.AdjustPentagonVertexOverage(adjustedResolution);

                // all Class III pentagon edges cross icosa edges
                // note that Class II pentagons have vertices on the edge,
                // not edge intersections
                if (IsResolutionClass3(resolution) && vert > start) {
                    // find hex2d of the two vertexes on the last face
                    FaceIJK tmpFijk = new FaceIJK(fijk);
                    Vec2d orig2d0 = lastFijk.Coord.ToVec2d();

                    int currentToLastDir = LookupTables.AdjacentFaceDirections[tmpFijk.Face, lastFijk.Face];

                    FaceOrientIJK fijkOrient = LookupTables.OrientedFaceNeighbours[tmpFijk.Face, currentToLastDir];
                    tmpFijk.Face = fijkOrient.Face;
                    CoordIJK ijk = new CoordIJK(tmpFijk.Coord);

                    // rotate and translate for adjacent face
                    for (int i = 0; i < fijkOrient.CounterClockwiseRotations; i += 1) ijk.RotateCounterClockwise();

                    ijk += fijkOrient.Translate * (LookupTables.UnitScaleByClass2Res[adjustedResolution] * 3);
                    ijk.Normalize();

                    Vec2d orig2d1 = ijk.ToVec2d();

                    // find the appropriate icosa face edge vertexes
                    int maxDim = LookupTables.MaxDistanceByClass2Res[adjustedResolution];
                    Vec2d v0 = new Vec2d(3.0 * maxDim, 0.0);
                    Vec2d v1 = new Vec2d(-1.5 * maxDim, 3.0 * M_SQRT3_2 * maxDim);
                    Vec2d v2 = new Vec2d(-1.5 * maxDim, -3.0 * M_SQRT3_2 * maxDim);

                    Vec2d edge0;
                    Vec2d edge1;

                    int adjacentFace = LookupTables.AdjacentFaceDirections[tmpFijk.Face, fijk.Face];
                    switch (adjacentFace) {
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
                            throw new NotSupportedException($"direction {adjacentFace} is not supported");
                    }

                    // find the intersection and add the lat/lon point to the result
                    Vec2d intersection = Vec2d.Intersect(orig2d0, orig2d1, edge0, edge1);
                    yield return intersection.ToFaceGeoCoord(tmpFijk.Face, adjustedResolution, true);
                }

                if (vert < start + NUM_PENT_VERTS) {
                    yield return fijk.Coord.ToVec2d().ToFaceGeoCoord(fijk.Face, adjustedResolution, true);
                }

                lastFijk = fijk;
            }
        }

        /// <summary>
        /// Generates the cell boundary in spherical coordinates for a cell given by a
        /// FaceIJK address at a specified resolution.
        /// </summary>
        /// <param name="resolution">The H3 resolution of the cell</param>
        /// <param name="start">The first topological vertex to return</param>
        /// <param name="length">The number of topological vertexes to return</param>
        /// <returns>The spherical coordinates of the cell boundary</returns>
        public IEnumerable<GeoCoord> GetBoundary(int resolution, int start, int length) {
            int adjustedResolution = resolution;
            FaceIJK[] verts = GetHexVertices(ref adjustedResolution);

            int additionalIteration = length == NUM_HEX_VERTS ? 1 : 0;

            int lastFace = -1;
            Overage lastOverage = Overage.None;

            for (int vert = start; vert < start + length + additionalIteration; vert += 1) {
                int v = vert % NUM_HEX_VERTS;

                FaceIJK face = new FaceIJK(verts[v]);
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
                    int maxDist = LookupTables.MaxDistanceByClass2Res[adjustedResolution];
                    Vec2d v0 = new Vec2d(3 * maxDist, 0);
                    Vec2d v1 = new Vec2d(-1.5 * maxDist, 3.0 * M_SQRT3_2 * maxDist);
                    Vec2d v2 = new Vec2d(-1.5 * maxDist, -3.0 * M_SQRT3_2 * maxDist);

                    Vec2d edge0;
                    Vec2d edge1;

                    int face2 = lastFace == Face ? face.Face : lastFace;

                    switch (LookupTables.AdjacentFaceDirections[Face, face2]) {
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
                        yield return intersection.ToFaceGeoCoord(Face, adjustedResolution, true);
                    }
                }

                // convert vertex to lat/lon and add to the result
                // vert == start + NUM_HEX_VERTS is only used to test for possible
                // intersection on last edge
                if (vert < start + NUM_HEX_VERTS) {
                    yield return face.Coord.ToVec2d().ToFaceGeoCoord(face.Face, adjustedResolution, true);
                }

                lastFace = face.Face;
                lastOverage = overage;
            }
        }

        public static bool operator ==(FaceIJK a, FaceIJK b) => a.Face == b.Face && a.Coord == b.Coord;

        public static bool operator !=(FaceIJK a, FaceIJK b) => a.Face != b.Face || a.Coord != b.Coord;

        public override bool Equals(object? other) => other is FaceIJK f && Face == f.Face && Coord == f.Coord;

        public override int GetHashCode() => HashCode.Combine(Face, Coord);
    }

}
