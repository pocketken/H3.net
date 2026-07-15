using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static H3.Constants;
using static H3.Utils;

#nullable enable

namespace H3.Model;

public struct FaceIJK : IEquatable<FaceIJK> {

    private const double THREE_M_SQRT32 = 3.0 * M_SQRT3_2;

    // Stack scratch-buffer size for boundary generation.  A full hexagon loop
    // emits at most NUM_HEX_VERTS (6) vertices + NUM_HEX_VERTS (6) edge-crossing
    // intersections = 12, and a pentagon loop at most 10, so 16 cannot overflow
    // for any in-contract length.
    private const int BoundaryStackBufferSize = 16;

    public int Face { get; set; }
    public CoordIJK Coord;

    public const int IJ = 1;
    public const int KI = 2;
    public const int JK = 3;

    public readonly BaseCellRotation? BaseCellRotation {
        get {
            if (Coord.I > MAX_FACE_COORD || Coord.J > MAX_FACE_COORD || Coord.K > MAX_FACE_COORD) return null;
            return LookupTables.FaceIjkBaseCells[Face, Coord.I, Coord.J, Coord.K];
        }
    }

    public FaceIJK() {
    }

    public FaceIJK(FaceIJK other) {
        Face = other.Face;
        Coord = other.Coord;
    }

    public FaceIJK(int face, CoordIJK coord) {
        Face = face;
        Coord = coord;
    }

    public static FaceIJK FromLatLng(double longitudeRadians, double latitudeRadians, int resolution) {
        unchecked {
            // Trig of the query point, computed once and reused: to build the 3D
            // unit vector for face selection, and (via the angle-subtraction
            // identities below) to form the projection azimuth without any
            // atan2 / sin / cos of the azimuth itself.
            var cosLat = Math.Cos(latitudeRadians);
            var sinLat = Math.Sin(latitudeRadians);
            var cosLon = Math.Cos(longitudeRadians);
            var sinLon = Math.Sin(longitudeRadians);

            // identical (same product order) to Vec3d.FromLonLat, so face
            // selection is bit-for-bit unchanged
            var v3d = new Vec3d(cosLon * cosLat, sinLon * cosLat, sinLat);
            var result = new FaceIJK();

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

            double x = 0;
            double y = 0;

            // dot == cos(angular distance) to the chosen face center; the square
            // distance between the two unit vectors is 2 - 2*dot.  The original
            // guard Math.Acos(dot) >= EPSILON is (EPSILON = 1e-16, cos(EPSILON)
            // rounds to exactly 1.0) identically `dot < 1.0`; at the face center
            // (dot == 1) the point projects to the origin.
            var dot = 1.0 - sqd / 2.0;
            if (dot < 1.0) {
                var face = result.Face;

                // Azimuth from the face center to the query point, kept as its raw
                // atan2 numerator (yAz) / denominator (xAz) — the north-referenced
                // spherical convention of AzimuthInRadians — instead of the angle:
                //   yAz = cos(lat) * sin(dLon)
                //   xAz = cos(cLat) * sin(lat) - sin(cLat) * cos(lat) * cos(dLon)
                // with dLon = lon - centerLon expanded by the angle-subtraction
                // identity from the query and (constant, per-face) center-longitude
                // trig, so no sin/cos of the difference is evaluated.
                var cosCLat = LookupTables.GeoFaceCenterCosLatitude[face];
                var sinCLat = LookupTables.GeoFaceCenterSinLatitude[face];
                var cosCLon = LookupTables.GeoFaceCenterCosLongitude[face];
                var sinCLon = LookupTables.GeoFaceCenterSinLongitude[face];

                var sinDLon = sinLon * cosCLon - cosLon * sinCLon;
                var cosDLon = cosLon * cosCLon + sinLon * sinCLon;

                var yAz = cosLat * sinDLon;
                var xAz = cosCLat * sinLat - sinCLat * cosLat * cosDLon;

                // The projected planar angle is theta = B - azimuth, where B is the
                // per-face (per-class) reference axis azimuth; its cos/sin come from
                // the precomputed cos/sin of B (round-14 tables) via the angle-
                // subtraction identity applied to cos(az) = xAz/rAz,
                // sin(az) = yAz/rAz.  The planar radius is
                // r = tan(distance)/RES0_U_GNOMONIC * M_SQRT7^resolution; since
                // rAz = sqrt(xAz^2 + yAz^2) = sin(distance) and
                // tan(distance)/sin(distance) = 1/cos(distance) = 1/dot, the rAz
                // normalisation and the tangent collapse into a single scale
                // s = M_SQRT7^resolution / (RES0_U_GNOMONIC * dot) — removing the
                // acos, tan, atan2 and both azimuth sin/cos entirely.
                var class3 = IsResolutionClass3(resolution);
                var cosB = class3 ? LookupTables.AxisAzimuthClass3Cos[face] : LookupTables.AxisAzimuthCos[face];
                var sinB = class3 ? LookupTables.AxisAzimuthClass3Sin[face] : LookupTables.AxisAzimuthSin[face];

                var s = LookupTables.Sqrt7PositivePowers[resolution] / (RES0_U_GNOMONIC * dot);
                x = s * (cosB * xAz + sinB * yAz);
                y = s * (sinB * xAz - cosB * yAz);
            }

            result.Coord = CoordIJK.FromVec2d(x, y);
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
    /// Span-filling variant of the vertex generator that writes the cell
    /// vertices into a caller-provided buffer instead of allocating a heap array,
    /// allowing boundary generation to use a <c>stackalloc</c> scratch buffer.
    /// Note that this modifies the address in place!
    /// </summary>
    private void GetVertices(CoordIJK[] class3Verts, CoordIJK[] class2Verts, ref int resolution, Span<FaceIJK> result) {
        var verts = IsResolutionClass3(resolution) ? class3Verts : class2Verts;
        Coord.DownAperture3CounterClockwise();
        Coord.DownAperture3Clockwise();

        // if res is Class III we need to add a cw aperture 7 to get to
        // icosahedral Class II
        if (IsResolutionClass3(resolution)) {
            Coord.DownAperture7Clockwise();
            resolution += 1;
        }

        for (var v = 0; v < verts.Length; v += 1) {
            result[v] = new FaceIJK(Face, (Coord + verts[v]).Normalize());
        }
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
    /// Span-filling variant of <see cref="GetHexVertices(ref int)"/>; the buffer
    /// must have room for at least <see cref="Constants.NUM_HEX_VERTS"/> vertices.
    /// </summary>
    private void GetHexVertices(ref int resolution, Span<FaceIJK> result) =>
        GetVertices(LookupTables.Class3HexVertices, LookupTables.Class2HexVertices, ref resolution, result);

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
    /// Span-filling variant of <see cref="GetPentagonVertices(ref int)"/>; the
    /// buffer must have room for at least <see cref="Constants.NUM_PENT_VERTS"/>
    /// vertices.
    /// </summary>
    private void GetPentagonVertices(ref int resolution, Span<FaceIJK> result) =>
        GetVertices(LookupTables.Class3PentagonVertices, LookupTables.Class2PentagonVertices, ref resolution, result);

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
        // For the normal contract (length <= NUM_PENT_VERTS) the core emits at
        // most NUM_PENT_VERTS vertices + NUM_PENT_VERTS edge-crossing
        // intersections = 10, which fits in the stack buffer.  Larger,
        // out-of-contract lengths fall back to a right-sized heap buffer so the
        // write can never overflow.
        if (length <= NUM_PENT_VERTS) {
            Span<LatLng> buffer = stackalloc LatLng[BoundaryStackBufferSize];
            var count = GetPentagonBoundary(resolution, start, length, buffer);
            var result = new LatLng[count];
            buffer[..count].CopyTo(result);
            return result;
        } else {
            var result = new LatLng[NUM_PENT_VERTS + length];
            var count = GetPentagonBoundary(resolution, start, length, result);
            if (count != result.Length) Array.Resize(ref result, count);
            return result;
        }
    }

    /// <summary>
    /// Span-filling core of <see cref="GetPentagonBoundary(int,int,int)"/>.  Writes
    /// the boundary vertices into <paramref name="destination"/> (which must have
    /// room for at least <c>length</c> vertices plus, for a full loop, up to
    /// <see cref="Constants.NUM_PENT_VERTS"/> edge-crossing vertices) and returns
    /// the number written.  Produces exactly the same vertex sequence as the
    /// enumerable overload.
    /// </summary>
    internal int GetPentagonBoundary(int resolution, int start, int length, Span<LatLng> destination) {
        unchecked {
            var count = 0;
            var adjustedResolution = resolution;
            FaceIJK centerIjk = new(this);
            Span<FaceIJK> verts = stackalloc FaceIJK[NUM_PENT_VERTS];
            centerIjk.GetPentagonVertices(ref adjustedResolution, verts);

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
            var intersection = new Vec2d();

            var fijk = new FaceIJK();
            var lastFijk = new FaceIJK();

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
                    lastFijk.Coord.ToVec2d(ref orig2d0);

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
                    ijk.ToVec2d(ref orig2d1);

                    // find the appropriate icosa face edge vertexes
                    var maxDist = LookupTables.MaxDistanceByClass2Res[adjustedResolution];
                    v0.X = 3 * maxDist;
                    v0.Y = 0;
                    v1.X = -1.5 * maxDist;

                    v1.Y = THREE_M_SQRT32 * maxDist;
                    v2.X = v1.X;
                    v2.Y = -THREE_M_SQRT32 * maxDist;

                    var adjacentFace = LookupTables.AdjacentFaceDirections[tmpFijk.Face, fijk.Face];
                    switch (adjacentFace) {
                        case IJ:
                            Vec2d.Intersect(orig2d0, orig2d1, v0, v1, ref intersection);
                            break;

                        case JK:
                            Vec2d.Intersect(orig2d0, orig2d1, v1, v2, ref intersection);
                            break;

                        case KI:
                            Vec2d.Intersect(orig2d0, orig2d1, v2, v0, ref intersection);
                            break;

                        default:
                            throw new NotSupportedException($"direction {adjacentFace} is not supported");
                    }

                    // find the intersection and add the lat/lon point to the result
                    destination[count++] = intersection.ToFaceLatLng(tmpFijk.Face, adjustedResolution, true);
                }

                if (vert < start + NUM_PENT_VERTS) {
                    destination[count++] = fijk.ToFaceLatLng(adjustedResolution, true);
                }

                lastFijk.Face = fijk.Face;
                lastFijk.Coord.I = fijk.Coord.I;
                lastFijk.Coord.J = fijk.Coord.J;
                lastFijk.Coord.K = fijk.Coord.K;
            }

            return count;
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
        // For the normal contract (length <= NUM_HEX_VERTS) the core emits at
        // most NUM_HEX_VERTS vertices + NUM_HEX_VERTS edge-crossing intersections
        // = 12, which fits in the stack buffer.  Larger, out-of-contract lengths
        // fall back to a right-sized heap buffer so the write can never overflow.
        if (length <= NUM_HEX_VERTS) {
            Span<LatLng> buffer = stackalloc LatLng[BoundaryStackBufferSize];
            var count = GetHexagonBoundary(resolution, start, length, buffer);
            var result = new LatLng[count];
            buffer[..count].CopyTo(result);
            return result;
        } else {
            var result = new LatLng[NUM_HEX_VERTS + length];
            var count = GetHexagonBoundary(resolution, start, length, result);
            if (count != result.Length) Array.Resize(ref result, count);
            return result;
        }
    }

    /// <summary>
    /// Span-filling core of <see cref="GetHexagonBoundary(int,int,int)"/>.  Writes
    /// the boundary vertices into <paramref name="destination"/> (which must have
    /// room for at least <c>length</c> vertices plus, for a full loop, up to
    /// <see cref="Constants.NUM_HEX_VERTS"/> edge-crossing vertices) and returns
    /// the number written.  Produces exactly the same vertex sequence as the
    /// enumerable overload.
    /// </summary>
    internal int GetHexagonBoundary(int resolution, int start, int length, Span<LatLng> destination) {
        unchecked {
            var count = 0;
            var adjustedResolution = resolution;
            FaceIJK centerIjk = new(this);
            Span<FaceIJK> verts = stackalloc FaceIJK[NUM_HEX_VERTS];
            centerIjk.GetHexVertices(ref adjustedResolution, verts);

            var additionalIteration = length == NUM_HEX_VERTS ? 1 : 0;

            var lastFace = -1;
            var lastOverage = Overage.None;

            Vec2d v0 = new();
            Vec2d v1 = new();
            Vec2d v2 = new();
            Vec2d orig2d0 = new();
            Vec2d orig2d1 = new();
            var intersection = new Vec2d();

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
                    verts[lastV].Coord.ToVec2d(ref orig2d0);
                    verts[v].Coord.ToVec2d(ref orig2d1);

                    // find the appropriate icosa face edge vertexes
                    var maxDist = LookupTables.MaxDistanceByClass2Res[adjustedResolution];
                    v0.X = 3 * maxDist;
                    v0.Y = 0;
                    v1.X = -1.5 * maxDist;
                    v1.Y = THREE_M_SQRT32 * maxDist;
                    v2.X = v1.X;
                    v2.Y = -THREE_M_SQRT32 * maxDist;

                    var face2 = lastFace == centerIjk.Face ? fijk.Face : lastFace;

                    switch (LookupTables.AdjacentFaceDirections[centerIjk.Face, face2]) {
                        case IJ:
                            Vec2d.Intersect(orig2d0, orig2d1, v0, v1, ref intersection);
                            break;

                        case JK:
                            Vec2d.Intersect(orig2d0, orig2d1, v1, v2, ref intersection);
                            break;

                        case KI:
                            Vec2d.Intersect(orig2d0, orig2d1, v2, v0, ref intersection);
                            break;

                        default:
                            throw new Exception("Unsupported direction");
                    }

                    var atVertex = orig2d0 == intersection || orig2d1 == intersection;
                    if (!atVertex) {
                        destination[count++] = intersection.ToFaceLatLng(centerIjk.Face, adjustedResolution, true);
                    }
                }

                // convert vertex to lat/lon and add to the result
                // vert == start + NUM_HEX_VERTS is only used to test for possible
                // intersection on last edge
                if (vert < start + NUM_HEX_VERTS) {
                    destination[count++] = fijk.ToFaceLatLng(adjustedResolution, true);
                }

                lastFace = fijk.Face;
                lastOverage = overage;
            }

            return count;
        }
    }

    public readonly LatLng ToLatLng(int resolution) {
        return ToFaceLatLng(resolution, false);
    }

    public readonly LatLng ToFaceLatLng(int resolution, bool isSubstrate) {
        var (x, y) = Coord.GetVec2dOrdinates();
        return ToFaceLatLng(x, y, Face, resolution, isSubstrate);
    }

    public static LatLng ToFaceLatLng(double x, double y, int face, int resolution, bool isSubstrate) {
        unchecked {
            // Faithful transliteration of libh3 v4.5.0 _hex2dToVec3 + vec3ToLatLng.
            // v4.5.0 replaced the spherical-law-of-cosines inverse projection with a
            // 3D-vector construction (tangent basis + linear combination + normalize),
            // so the port must follow it — and perform the same operations in the same
            // order — to stay bit-for-bit with the reference boundaries and centers.
            var faceCenter = LookupTables.FaceCenters[face];

            // calculate (r, theta) in hex2d
            var r = Math.Sqrt(x * x + y * y);
            if (r < EPSILON) {
                return faceCenter.ToLatLng();
            }

            var theta = Math.Atan2(y, x);

            // scale for current resolution length u
            for (var i = 0; i < resolution; i++) r *= M_RSQRT7;

            // scale accordingly if this is a substrate grid
            if (isSubstrate) {
                r *= M_ONETHIRD;
                if (IsResolutionClass3(resolution)) r *= M_RSQRT7;
            }

            r *= RES0_U_GNOMONIC;

            // perform inverse gnomonic scaling of r
            r = Math.Atan(r);

            // adjust theta for Class III; if a substrate grid it's already adjusted
            if (!isSubstrate && IsResolutionClass3(resolution))
                theta = PosAngleRads(theta + M_AP7_ROT_RADS);

            // find theta as an azimuth
            theta = PosAngleRads(LookupTables.AxisAzimuths[face] - theta);

            // now find the point at (r, theta) from the face center
            TangentBasis(faceCenter, out var north, out var east);
            var dir = Vec3d.LinComb(Math.Cos(theta), north, Math.Sin(theta), east);
            var v3 = Vec3d.LinComb(Math.Cos(r), faceCenter, Math.Sin(r), dir).Normalize();

            return v3.ToLatLng();
        }
    }

    /// <summary>
    /// Local north and east directions on the tangent plane at a point on the unit
    /// sphere (libh3 <c>_vec3TangentBasis</c>).  Not valid at a pole, but icosahedron
    /// face centers are never at the poles.
    /// </summary>
    private static void TangentBasis(Vec3d p, out Vec3d north, out Vec3d east) {
        var northPole = new Vec3d(0.0, 0.0, 1.0);
        north = Vec3d.LinComb(1.0, northPole, -Vec3d.Dot(northPole, p), p).Normalize();
        east = Vec3d.Cross(north, p);
    }

    /// <summary>Normalizes an angle in radians to the range [0, 2*pi) (libh3 <c>_posAngleRads</c>).</summary>
    private static double PosAngleRads(double rads) {
        var tmp = rads < 0.0 ? rads + M_2PI : rads;
        if (rads >= M_2PI) tmp -= M_2PI;
        return tmp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(FaceIJK other) => Face == other.Face && Coord == other.Coord;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(FaceIJK a, FaceIJK b) =>
        a.Face == b.Face && a.Coord == b.Coord;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(FaceIJK a, FaceIJK b) =>
        a.Face != b.Face || a.Coord != b.Coord;

    public override readonly bool Equals(object? other) => other is FaceIJK f && this == f;

    public override readonly int GetHashCode() => HashCode.Combine(Face, Coord);

}