﻿using System;
using System.Collections.Generic;
using System.Linq;
using H3.Model;
using static H3.Constants;

#nullable enable

namespace H3.Extensions {

    /// <summary>
    /// Extends the H3Index class with support for vertex functionality.
    /// </summary>
    public static class H3VertexExtensions {
        public const int InvalidVertex = -1;
        private const int DirectionIndexOffset = 2;

        /// <summary>
        /// Hexagon direction to vertex number relationships (same face).
        /// Note that we don't use direction 0 (center).
        /// </summary>
        private static readonly int[] HexDirectionToVertexNum = new int[7] {
            7, 3, 1, 2, 5, 4, 0
        };

        /// <summary>
        /// Pentagon direction to vertex number relationships (same face).
        /// Note that we don't use directions 0 (center) or 1 (deleted K axis).
        /// </summary>
        private static readonly int[] PentagonDirectionToVertexNum = new int[7] {
            7, 7, 1, 2, 4, 3, 0
        };

        /// <summary>
        /// Vertex number to hexagon direction relationships (same face).
        /// </summary>
        private static readonly Direction[] HexVertexNumToDirection = new Direction[NUM_HEX_VERTS] {
            Direction.IJ,
            Direction.J,
            Direction.JK,
            Direction.K,
            Direction.IK,
            Direction.I
        };

        /// <summary>
        /// Vertex number to pentagon direction relationships (same face).
        /// </summary>
        private static readonly Direction[] PentagonVertexNumToDirection = new Direction[NUM_PENT_VERTS] {
            Direction.IJ,
            Direction.J,
            Direction.JK,
            Direction.IK,
            Direction.I
        };

        /// <summary>
        /// Directions in CCW order.
        /// </summary>
        private static readonly Direction[] HexDirections = new Direction[NUM_HEX_VERTS] {
            Direction.J,
            Direction.JK,
            Direction.K,
            Direction.IK,
            Direction.I,
            Direction.IJ
        };
        private static readonly int[] HexNeighbourDirections = new int[7] {
            7, 5, 3, 4, 1, 0, 2
        };

        /// <summary>
        /// Get the number of CCW rotations of the cell's vertex numbers
        /// compared to the directional layout of its neighbors.
        /// </summary>
        /// <param name="index">H3 index</param>
        /// <returns>Number of CCW rotations</returns>
        private static int VertexRotations(this H3Index index) {
            var fijk = index.ToFaceIJK();
            var baseFijk = index.BaseCell.Home;

            int ccwRotations = BaseCellRotation
                .GetCounterClockwiseRotationsForBaseCell(index.BaseCellNumber, fijk.Face);

            if (index.BaseCell.IsPentagon) {
                PentagonDirectionToFaceMapping? dirFaces = null;

                // find the appropriate direction-to-face mapping
                for (int p = 0; p < NUM_PENTAGONS; p += 1) {
                    if (LookupTables.PentagonDirectionFaces[p].BaseCellNumber == index.BaseCellNumber) {
                        dirFaces = LookupTables.PentagonDirectionFaces[p];
                        break;
                    }
                }

                if (dirFaces == null) {
                    throw new Exception("cant find pentagon direction to face mapping");
                }

                // additional CCW rotation for polar neighbors or IK neighbors
                if ((fijk.Face != baseFijk.Face && index.BaseCell.IsPolarPentagon) || fijk.Face == dirFaces.Faces[(int)Direction.IK - DirectionIndexOffset]) {
                    ccwRotations = (ccwRotations + 1) % 6;
                }

                // check whether the cell crosses a deleted pentagon subsequence
                if (index.LeadingNonZeroDirection == Direction.JK && fijk.Face == dirFaces.Faces[(int)Direction.IK - DirectionIndexOffset]) {
                    // crosses from JK to IK: Rotate CW
                    ccwRotations = (ccwRotations + 5) % 6;
                } else if (index.LeadingNonZeroDirection == Direction.IK && fijk.Face == dirFaces.Faces[(int)Direction.JK - DirectionIndexOffset]) {
                    // crosses from IK to JK: Rotate CCW
                    ccwRotations = (ccwRotations + 1) % 6;
                }
            }

            return ccwRotations;
        }

        /// <summary>
        /// Get the first vertex number for a given direction. The neighbor in this
        /// direction is located between this vertex number and the next number in
        /// sequence.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <returns>The number for the first topological vertex, or INVALID_VERTEX_NUM
        /// if the direction is not valid for this cell</returns>
        public static int GetVertexNumberForDirection(this H3Index origin, Direction direction) {
            bool isPentagon = origin.IsPentagon;

            // check for invalid directions
            if (direction == Direction.Center || direction >= Direction.Invalid || (isPentagon && direction == Direction.K)) {
                return InvalidVertex;
            }

            // Determine the vertex rotations for this cell
            int rotations = VertexRotations(origin);

            // Find the appropriate vertex, rotating CCW if necessary
            return isPentagon
                ? (PentagonDirectionToVertexNum[(int)direction] + NUM_PENT_VERTS - rotations) % NUM_PENT_VERTS
                : (HexDirectionToVertexNum[(int)direction] + NUM_HEX_VERTS - rotations) % NUM_HEX_VERTS;
        }

        /// <summary>
        /// Get the direction for a given vertex number. This returns the direction for
        /// the neighbor between the given vertex number and the next number in sequence.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="vertexNum"></param>
        /// <returns>The direction for this vertex, or INVALID_DIGIT if the vertex
        /// number is invalid.</returns>
        public static Direction GetDirectionForVertexNumber(this H3Index origin, int vertexNum) {
            bool isPentagon = origin.IsPentagon;

            // check for invalid vertexes
            if (vertexNum < 0 || vertexNum > (isPentagon ? NUM_PENT_VERTS : NUM_HEX_VERTS) - 1) {
                return Direction.Invalid;
            }

            // Determine the vertex rotations for this cell
            int rotations = VertexRotations(origin);

            // Find the appropriate direction, rotating CW if necessary
            return isPentagon
                ? PentagonVertexNumToDirection[(vertexNum + rotations) % NUM_PENT_VERTS]
                : HexVertexNumToDirection[(vertexNum + rotations) % NUM_HEX_VERTS];
        }

        /// <summary>
        /// Get a single vertex for a given cell as an H3 index, or
        /// H3Index.Invalid if the vertex is invalid.
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="vertexNum"></param>
        /// <returns></returns>
        public static H3Index GetVertexIndex(this H3Index cell, int vertexNum) {
            bool cellIsPentagon = cell.IsPentagon;
            int cellNumVerts = cellIsPentagon ? NUM_PENT_VERTS : NUM_HEX_VERTS;
            int res = cell.Resolution;

            // Check for invalid vertexes
            if (vertexNum < 0 || vertexNum > cellNumVerts - 1) {
                return H3Index.Invalid;
            }

            // Default the owner and vertex number to the input cell
            H3Index owner = cell;
            int ownerVertexNum = vertexNum;

            // Determine the owner, looking at the three cells that share the vertex.
            // By convention, the owner is the cell with the lowest numerical index.

            // If the cell is the center child of its parent, it will always have
            // the lowest index of any neighbor, so we can skip determining the owner
            if (res == 0 || cell.Direction != Direction.Center) {
                // Get the left neighbor of the vertex, with its rotations
                Direction left = GetDirectionForVertexNumber(cell, vertexNum);

                if (left == Direction.Invalid) {
                    return H3Index.Invalid;
                }

                var (leftNeighbour, lRotations) = cell.GetDirectNeighbour(left);

                // Set to owner if lowest index
                if (leftNeighbour < owner) {
                    owner = leftNeighbour;
                }

                // As above, skip the right neighbor if the left is known lowest
                if (res == 0 || leftNeighbour.GetDirectionForResolution(res) != Direction.Center) {
                    // Get the right neighbor of the vertex, with its rotations
                    // Note that vertex - 1 is the right side, as vertex numbers are CCW
                    Direction right = GetDirectionForVertexNumber(cell, (vertexNum - 1 + cellNumVerts) % cellNumVerts);

                    if (right == Direction.Invalid) {
                        return H3Index.Invalid;
                    }

                    var (rightNeighbour, rRotations) = cell.GetDirectNeighbour(right);

                    // Set to owner if lowest index
                    if (rightNeighbour < owner) {
                        owner = rightNeighbour;
                        Direction dir = owner.IsPentagon
                            ? owner.DirectionForNeighbour(cell)
                            : HexDirections[(HexNeighbourDirections[(int)right] + rRotations) % NUM_HEX_VERTS];
                        ownerVertexNum = GetVertexNumberForDirection(owner, dir);
                    }
                }

                // Determine the vertex number for the left neighbor
                if (owner == leftNeighbour) {
                    bool ownerIsPentagon = owner.IsPentagon;
                    Direction dir = ownerIsPentagon
                        ? owner.DirectionForNeighbour(cell)
                        : HexDirections[(HexNeighbourDirections[(int)left] + lRotations) % NUM_HEX_VERTS];

                    // For the left neighbor, we need the second vertex of the
                    // edge, which may involve looping around the vertex nums
                    ownerVertexNum = GetVertexNumberForDirection(owner, dir) + 1;

                    if (ownerVertexNum == NUM_HEX_VERTS || (ownerIsPentagon && ownerVertexNum == NUM_PENT_VERTS)) {
                        ownerVertexNum = 0;
                    }
                }
            }

            // Create the vertex index
            return new H3Index(owner) {
                Mode = Mode.Vertex,
                ReservedBits = ownerVertexNum
            };
        }

        /// <summary>
        /// Get all vertexes for the given cell.
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public static IEnumerable<H3Index> GetVertexIndicies(this H3Index cell) {
            int count = cell.IsPentagon ? NUM_PENT_VERTS : NUM_HEX_VERTS;
            for (int i = 0; i < count; i += 1) {
                yield return cell.GetVertexIndex(i);
            }
        }

        /// <summary>
        /// Get the geocoordinates of a H3 vertex index.
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        public static GeoCoord VertexToGeoCoord(this H3Index vertex) {
            // Get the vertex number and owner from the vertex
            int vertexNum = vertex.ReservedBits;
            H3Index owner = new(vertex) {
                Mode = Mode.Cell,
                ReservedBits = 0
            };

            FaceIJK fijk = owner.ToFaceIJK();
            int resolution = owner.Resolution;

            var vertices = owner.IsPentagon
                ? fijk.GetPentagonBoundary(resolution, vertexNum, 1)
                : fijk.GetHexagonBoundary(resolution, vertexNum, 1);

            return vertices.First();
        }

        /// <summary>
        /// Whether the input is a valid H3 vertex index.
        /// </summary>
        /// <param name="vertex">H3 index possibly describing a vertex</param>
        /// <returns>Whether the input is valid</returns>
        public static bool IsValidVertex(this H3Index vertex) {
            if (vertex.Mode != Mode.Vertex) {
                return false;
            }

            int vertexNum = vertex.ReservedBits;
            H3Index owner = new(vertex) {
                Mode = Mode.Cell,
                ReservedBits = 0
            };

            if (!owner.IsValid) {
                return false;
            }

            // The easiest way to ensure that the owner + vertex number is valid,
            // and that the vertex is canonical, is to recreate and compare.
            H3Index canonical = owner.GetVertexIndex(vertexNum);
            return vertex == canonical;
        }

    }
}