using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static H3.Constants;
using static H3.Utils;

#nullable enable

namespace H3.Model; 

public sealed class FaceIJK {

    private const double THREE_M_SQRT32 = 3.0 * M_SQRT3_2;

    public int Face { get; set; }
    public CoordIJK Coord { get; set; } = new(0, 0, 0);

    public const int IJ = 1;
    public const int KI = 2;
    public const int JK = 3;

    public BaseCellRotation? BaseCellRotation {
        get {
            if (Coord.I > MAX_FACE_COORD || Coord.J > MAX_FACE_COORD || Coord.K > MAX_FACE_COORD) return null;
            return LookupTables.FaceIjkBaseCells[Face, Coord.I, Coord.J, Coord.K];
        }
    }

    public FaceIJK() {
    }

    public FaceIJK(FaceIJK other) {
        Face = other.Face;
        Coord = new CoordIJK(other.Coord);
    }

    public FaceIJK(int face, CoordIJK coord) {
        Face = face;
        Coord = new CoordIJK(coord);
    }

    public static FaceIJK FromGeoCoord(double longitudeRadians, double latitudeRadians, int resolution,
        FaceIJK? toUpdate = default, Vec3d? workVec3d = default) {
        unchecked {
            var v3d = Vec3d.FromLonLat(longitudeRadians, latitudeRadians, workVec3d);
            var result = toUpdate ?? new FaceIJK();

            result.Face = 0;
            result.Coord.I = 0;
            result.Coord.J = 0;
            result.Coord.K = 0;

            var sqd = v3d.PointSquareDistance(LookupTables.FaceCenters[0]);
            for (var f = 1; f < NUM_ICOSA_FACES; f += 1) {
                var sqdT = v3d.PointSquareDistance(LookupTables.FaceCenters[f]);
                if (sqdT >= sqd)
                    continue;

                result.Face = f;
                sqd = sqdT;
            }

            var r = Math.Acos(1 - sqd / 2);
            double x = 0;
            double y = 0;

            if (r >= EPSILON) {
                var center = LookupTables.GeoFaceCenters[result.Face];
                var az = NormalizeAngle(AzimuthInRadians(center.Longitude, center.Latitude, longitudeRadians,
                    latitudeRadians));
                var theta = NormalizeAngle(LookupTables.AxisAzimuths[result.Face] - az);

                if (IsResolutionClass3(resolution)) theta = NormalizeAngle(theta - M_AP7_ROT_RADS);

                r = Math.Tan(r) / RES0_U_GNOMONIC;
                for (var i = 0; i < resolution; i += 1) r *= M_SQRT7;

                x = r * Math.Cos(theta);
                y = r * Math.Sin(theta);
            }

            CoordIJK.FromVec2d(x, y, result.Coord);
            return result;
        }
    }

    // TODO provide version that reuses result array
    private FaceIJK[] GetVertices(CoordIJK[] class3Verts, CoordIJK[] class2Verts, ref int resolution) {
        var verts = IsResolutionClass3(resolution) ? class3Verts : class2Verts;
        Coord.DownAperture3CounterClockwise();
        Coord.DownAperture3Clockwise();

        // if res is Class III we need to add a cw aperture 7 to get to
        // icosahedral Class II
        if (IsResolutionClass3(resolution)) {
            Coord.DownAperture7Clockwise();
            resolution += 1;
        }

        var result = new FaceIJK[verts.Length];
        for (var v = 0; v < verts.Length; v += 1) {
            result[v] = new FaceIJK(Face, (Coord + verts[v]).Normalize());
        }

        return result;
    }

    /// <summary>
    /// Get the vertices of a cell as substrate FaceIJK addresses.  Note that this modifies
    /// the address in place!
    /// </summary>
    /// <param name="resolution">The H3 resolution of the cell. This may be adjusted if
    /// necessary for the substrate grid resolution.</param>
    /// <returns>cell vertices</returns>
    public FaceIJK[] GetHexVertices(ref int resolution) =>
        GetVertices(LookupTables.Class3HexVertices, LookupTables.Class2HexVertices, ref resolution);

    /// <summary>
    /// Get the vertices of a pentagon cell as substrate FaceIJK addresses.  Note that this
    /// modifies the address in place!
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Overage AdjustOverageClass2(int resolution, bool pentagonLeading4, bool isSubstrate) {
        unchecked {
            var overage = Overage.None;

            var maxDist = LookupTables.MaxDistanceByClass2Res[resolution];
            if (isSubstrate) maxDist *= 3;

            var sum = Coord.I + Coord.J + Coord.K;
            if (isSubstrate && sum == maxDist) {
                overage = Overage.FaceEdge;
            } else if (sum > maxDist) {
                overage = Overage.NewFace;

                var orientedFace = LookupTables.OrientedFaceNeighbours[Face, IJ];
                if (Coord.K > 0) {
                    if (Coord.J > 0) {
                        orientedFace = LookupTables.OrientedFaceNeighbours[Face, JK];
                    } else {
                        orientedFace = LookupTables.OrientedFaceNeighbours[Face, KI];

                        // adjust for the pentagonal missing sequence
                        if (pentagonLeading4) {
                            // translate origin to center of pentagon, rotate to adjust for the missing sequence
                            // and translate the origin back to the center of the triangle
                            Coord.I -= maxDist;
                            Coord.RotateClockwise();
                            Coord.I += maxDist;
                        }
                    }
                }

                Face = orientedFace.Face;

                // rotate and translate for adjacent face
                for (var i = 0; i < orientedFace.CounterClockwiseRotations; i += 1) {
                    Coord.RotateCounterClockwise();
                }

                var unitScale = LookupTables.UnitScaleByClass2Res[resolution];
                if (isSubstrate) unitScale *= 3;
                Coord.I += orientedFace.Translate.I * unitScale;
                Coord.J += orientedFace.Translate.J * unitScale;
                Coord.K += orientedFace.Translate.K * unitScale;
                Coord.Normalize();

                // overage points on pentagon boundaries can end up on edges
                if (isSubstrate && Coord.I + Coord.J + Coord.K == maxDist) {
                    overage = Overage.FaceEdge;
                }
            }

            return overage;
        }
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
    public IEnumerable<LatLng> GetPentagonBoundary(int resolution, int start, int length) {
        unchecked {
            var adjustedResolution = resolution;
            FaceIJK centerIjk = new(this);
            var verts = centerIjk.GetPentagonVertices(ref adjustedResolution);

            // If we're returning the entire loop, we need one more iteration in case
            // of a distortion vertex on the last edge
            var additionalIteration = length == NUM_PENT_VERTS ? 1 : 0;

            // convert each vertex to lat/lon
            // adjust the face of each vertex as appropriate and introduce
            // edge-crossing vertices as needed
            Vec2d v0 = new();
            Vec2d v1 = new();
            Vec2d v2 = new();
            Vec2d orig2d0 = new();
            Vec2d orig2d1 = new();

            var fijk = new FaceIJK();
            FaceIJK lastFijk = new();

            for (var vert = start; vert < start + length + additionalIteration; vert += 1) {
                var v = vert % NUM_PENT_VERTS;

                fijk.Face = verts[v].Face;
                fijk.Coord.I = verts[v].Coord.I;
                fijk.Coord.J = verts[v].Coord.J;
                fijk.Coord.K = verts[v].Coord.K;
                fijk.AdjustPentagonVertexOverage(adjustedResolution);

                // all Class III pentagon edges cross icosa edges
                // note that Class II pentagons have vertices on the edge,
                // not edge intersections
                if (IsResolutionClass3(resolution) && vert > start) {
                    // find hex2d of the two vertexes on the last face
                    FaceIJK tmpFijk = new(fijk);
                    lastFijk.Coord.ToVec2d(orig2d0);

                    var currentToLastDir = LookupTables.AdjacentFaceDirections[tmpFijk.Face, lastFijk.Face];

                    var fijkOrient = LookupTables.OrientedFaceNeighbours[tmpFijk.Face, currentToLastDir];
                    tmpFijk.Face = fijkOrient.Face;
                    CoordIJK ijk = new(tmpFijk.Coord);

                    // rotate and translate for adjacent face
                    for (var i = 0; i < fijkOrient.CounterClockwiseRotations; i += 1) ijk.RotateCounterClockwise();

                    var scale = LookupTables.UnitScaleByClass2Res[adjustedResolution] * 3;
                    ijk.I += fijkOrient.Translate.I * scale;
                    ijk.J += fijkOrient.Translate.J * scale;
                    ijk.K += fijkOrient.Translate.K * scale;
                    ijk.Normalize();

                    ijk.ToVec2d(orig2d1);

                    // find the appropriate icosa face edge vertexes
                    var maxDist = LookupTables.MaxDistanceByClass2Res[adjustedResolution];
                    v0.X = 3 * maxDist;
                    v0.Y = 0;
                    v1.X = -1.5 * maxDist;

                    v1.Y = THREE_M_SQRT32 * maxDist;
                    v2.X = v1.X;
                    v2.Y = -THREE_M_SQRT32 * maxDist;

                    Vec2d intersection;
                    Vec2d edge0;
                    Vec2d edge1;

                    var adjacentFace = LookupTables.AdjacentFaceDirections[tmpFijk.Face, fijk.Face];
                    switch (adjacentFace) {
                        case IJ:
                            edge0 = v0;
                            edge1 = v1;
                            intersection = v2;
                            break;

                        case JK:
                            edge0 = v1;
                            edge1 = v2;
                            intersection = v0;
                            break;

                        case KI:
                            edge0 = v2;
                            edge1 = v0;
                            intersection = v1;
                            break;

                        default:
                            throw new NotSupportedException($"direction {adjacentFace} is not supported");
                    }

                    // find the intersection and add the lat/lon point to the result
                    Vec2d.Intersect(orig2d0, orig2d1, edge0, edge1, intersection);
                    yield return intersection.ToFaceGeoCoord(tmpFijk.Face, adjustedResolution, true);
                }

                if (vert < start + NUM_PENT_VERTS) {
                    yield return fijk.ToFaceGeoCoord(adjustedResolution, true);
                }

                lastFijk.Face = fijk.Face;
                lastFijk.Coord.I = fijk.Coord.I;
                lastFijk.Coord.J = fijk.Coord.J;
                lastFijk.Coord.K = fijk.Coord.K;
            }
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
    public IEnumerable<LatLng> GetHexagonBoundary(int resolution, int start, int length) {
        unchecked {
            var adjustedResolution = resolution;
            FaceIJK centerIjk = new(this);
            var verts = centerIjk.GetHexVertices(ref adjustedResolution);

            var additionalIteration = length == NUM_HEX_VERTS ? 1 : 0;

            var lastFace = -1;
            var lastOverage = Overage.None;

            Vec2d v0 = new();
            Vec2d v1 = new();
            Vec2d v2 = new();
            Vec2d orig2d0 = new();
            Vec2d orig2d1 = new();

            var fijk = new FaceIJK();

            for (var vert = start; vert < start + length + additionalIteration; vert += 1) {
                var v = vert % NUM_HEX_VERTS;

                fijk.Face = verts[v].Face;
                fijk.Coord.I = verts[v].Coord.I;
                fijk.Coord.J = verts[v].Coord.J;
                fijk.Coord.K = verts[v].Coord.K;

                var overage = fijk.AdjustOverageClass2(adjustedResolution, false, true);

                /*
                    Check for edge-crossing. Each face of the underlying icosahedron is a
                    different projection plane. So if an edge of the cell crosses an
                    icosahedron edge, an additional vertex must be introduced at that
                    intersection point. Then each half of the cell edge can be projected
                    to geographic coordinates using the appropriate icosahedron face
                    projection. Note that Class II cell edges have vertices on the face
                    edge, with no edge line intersections.
                */
                if (IsResolutionClass3(resolution) && vert > start && fijk.Face != lastFace &&
                    lastOverage != Overage.FaceEdge) {
                    // find hex2d of the two vertexes on original face
                    var lastV = (v + 5) % NUM_HEX_VERTS;
                    verts[lastV].Coord.ToVec2d(orig2d0);
                    verts[v].Coord.ToVec2d(orig2d1);

                    // find the appropriate icosa face edge vertexes
                    var maxDist = LookupTables.MaxDistanceByClass2Res[adjustedResolution];
                    v0.X = 3 * maxDist;
                    v0.Y = 0;
                    v1.X = -1.5 * maxDist;
                    v1.Y = THREE_M_SQRT32 * maxDist;
                    v2.X = v1.X;
                    v2.Y = -THREE_M_SQRT32 * maxDist;

                    var face2 = lastFace == centerIjk.Face ? fijk.Face : lastFace;

                    Vec2d intersection;
                    Vec2d edge0;
                    Vec2d edge1;

                    switch (LookupTables.AdjacentFaceDirections[centerIjk.Face, face2]) {
                        case IJ:
                            edge0 = v0;
                            edge1 = v1;
                            intersection = v2;
                            break;

                        case JK:
                            edge0 = v1;
                            edge1 = v2;
                            intersection = v0;
                            break;

                        case KI:
                            edge0 = v2;
                            edge1 = v0;
                            intersection = v1;
                            break;

                        default:
                            throw new Exception("Unsupported direction");
                    }

                    Vec2d.Intersect(orig2d0, orig2d1, edge0, edge1, intersection);
                    var atVertex = orig2d0 == intersection || orig2d1 == intersection;
                    if (!atVertex) {
                        yield return intersection.ToFaceGeoCoord(centerIjk.Face, adjustedResolution, true);
                    }
                }

                // convert vertex to lat/lon and add to the result
                // vert == start + NUM_HEX_VERTS is only used to test for possible
                // intersection on last edge
                if (vert < start + NUM_HEX_VERTS) {
                    yield return fijk.ToFaceGeoCoord(adjustedResolution, true);
                }

                lastFace = fijk.Face;
                lastOverage = overage;
            }
        }
    }

    public LatLng ToGeoCoord(int resolution) {
        return ToFaceGeoCoord(resolution, false);
    }

    public LatLng ToFaceGeoCoord(int resolution, bool isSubstrate) {
        var (x, y) = Coord.GetVec2dOrdinates();
        return ToFaceGeoCoord(x, y, Face, resolution, isSubstrate);
    }

    public static LatLng ToFaceGeoCoord(double x, double y, int face, int resolution, bool isSubstrate) {
        unchecked {
            var r = Math.Sqrt(x * x + y * y);
            if (r < EPSILON) {
                return new LatLng(LookupTables.GeoFaceCenters[face]);
            }

            var theta = Math.Atan2(y, x);

            for (var i = 0; i < resolution; i += 1) r /= M_SQRT7;
            if (isSubstrate) {
                r /= 3.0;
                if (IsResolutionClass3(resolution)) r /= M_SQRT7;
            }

            r = Math.Atan(r * RES0_U_GNOMONIC);
            if (!isSubstrate && IsResolutionClass3(resolution)) {
                theta = NormalizeAngle(theta + M_AP7_ROT_RADS);
            }

            theta = NormalizeAngle(LookupTables.AxisAzimuths[face] - theta);
            return LatLng.ForAzimuthDistanceInRadians(LookupTables.GeoFaceCenters[face], theta, r);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(FaceIJK? a, FaceIJK? b) {
        if (a is null) return b is null;
        if (b is null) return false;
        return a.Face == b.Face && a.Coord == b.Coord;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(FaceIJK? a, FaceIJK? b) {
        if (a is null) return b is not null;
        if (b is null) return true;
        return a.Face != b.Face || a.Coord != b.Coord;
    }

    public override bool Equals(object? other) => other is FaceIJK f && this == f;

    public override int GetHashCode() => HashCode.Combine(Face, Coord);

}