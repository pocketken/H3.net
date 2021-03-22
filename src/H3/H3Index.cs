using System;
using System.Globalization;
using H3.Model;
using static H3.Constants;
using static H3.Utils;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;

#nullable enable

namespace H3 {

    public class H3Index : IComparable<H3Index> {

        #region constants

        public const int H3_MAX_OFFSET = 63;
        public const int H3_MODE_OFFSET = 59;
        public const int H3_BC_OFFSET = 45;
        public const int H3_RES_OFFSET = 52;
        public const int H3_RESERVED_OFFSET = 56;
        public const int H3_PER_DIGIT_OFFSET = 3;
        public const ulong H3_HIGH_BIT_MASK = (ulong)1 << H3_MAX_OFFSET;
        public const ulong H3_HIGH_BIT_MASK_NEGATIVE = ~H3_HIGH_BIT_MASK;
        public const ulong H3_MODE_MASK = (ulong)15 << H3_MODE_OFFSET;
        public const ulong H3_MODE_MASK_NEGATIVE = ~H3_MODE_MASK;
        public const ulong H3_BC_MASK = (ulong)127 << H3_BC_OFFSET;
        public const ulong H3_BC_MASK_NEGATIVE = ~H3_BC_MASK;
        public const ulong H3_RES_MASK = (ulong)15 << H3_RES_OFFSET;
        public const ulong H3_RES_MASK_NEGATIVE = ~H3_RES_MASK;
        public const ulong H3_RESERVED_MASK = (ulong)7 << H3_RESERVED_OFFSET;
        public const ulong H3_RESERVED_MASK_NEGATIVE = ~H3_RESERVED_MASK;
        public const ulong H3_DIGIT_MASK = 7;

        /// <summary>
        /// H3 index with mode 0, res 0, base cell 0, and 7 for all index digits.
        /// Typically used to initialize the creation of an H3 cell index, which
        /// expects all direction digits to be 7 beyond the cell's resolution.
        /// </summary>
        private const ulong H3_INIT = 35184372088831UL;

        /// <summary>
        /// H3 index with a value of 0; aka H3_NULL
        /// </summary>
        public static readonly H3Index Invalid = new(0);

        #endregion constants

        #region properties

        private ulong Value { get; set; } = 0;

        public BaseCell BaseCell => LookupTables.BaseCells[BaseCellNumber];

        /// <summary>
        /// The highest bit value of the index.
        /// </summary>
        public int HighBit {
            get => (int)((Value & H3_HIGH_BIT_MASK) >> H3_MAX_OFFSET);
            set => Value = (Value & H3_HIGH_BIT_MASK_NEGATIVE) | ((ulong)value << H3_MAX_OFFSET);
        }

        /// <summary>
        /// The Mode of the index.
        /// </summary>
        public Mode Mode {
            get => (Mode)((Value & H3_MODE_MASK) >> H3_MODE_OFFSET);
            set => Value = (Value & H3_MODE_MASK_NEGATIVE) | ((ulong)value << H3_MODE_OFFSET);
        }

        /// <summary>
        /// The base cell number of the index.  Must be >= 0 < NUM_BASE_CELLS
        /// </summary>
        public int BaseCellNumber {
            get => (int)((Value & H3_BC_MASK) >> H3_BC_OFFSET);
            set => Value = (Value & H3_BC_MASK_NEGATIVE) | ((ulong)value << H3_BC_OFFSET);
        }

        /// <summary>
        /// The resolution of the index.  Must be >= 0 <= MAX_H3_RES
        /// </summary>
        public int Resolution {
            get => (int)((Value & H3_RES_MASK) >> H3_RES_OFFSET);
            set => Value = (Value & H3_RES_MASK_NEGATIVE) | ((ulong)value << H3_RES_OFFSET);
        }

        /// <summary>
        /// The Direction "digit" for the index at its base resolution, e.g.
        /// this is the result of <code>GetDirectionForResolution(Resolution)</code>
        /// </summary>
        public Direction Direction {
            get => GetDirectionForResolution(Resolution);
            set => SetDirectionForResolution(Resolution, value);
        }

        /// <summary>
        /// The reserved bit value of the index.  Setting to non-zero may invalidate
        /// the index.
        /// </summary>
        public int ReservedBits {
            get => (int)((Value & H3_RESERVED_MASK) >> H3_RESERVED_OFFSET);
            set => Value = (Value & H3_RESERVED_MASK_NEGATIVE) | ((ulong)value << H3_RESERVED_OFFSET);
        }

        /// <summary>
        /// Whether or not the index is valid.
        /// </summary>
        public bool IsValid {
            get {
                if (HighBit != 0) return false;
                if (Mode != Mode.Hexagon) return false;
                if (ReservedBits != 0) return false;

                int baseCell = BaseCellNumber;
                if (baseCell < 0 || baseCell >= NUM_BASE_CELLS) return false;

                int resolution = Resolution;
                if (resolution < 0 || resolution > MAX_H3_RES) return false;

                bool foundFirstNonZeroDigit = false;
                for (int r = 1; r <= resolution; r += 1) {
                    Direction idx = GetDirectionForResolution(r);

                    if (!foundFirstNonZeroDigit && idx != Direction.Center) {
                        foundFirstNonZeroDigit = true;
                        if (LookupTables.BaseCells[baseCell].IsPentagon && idx == Direction.K) {
                            return false;
                        }
                    }

                    if (idx < Direction.Center || idx >= Direction.Invalid) {
                        return false;
                    }
                }

                for (int r = resolution + 1; r <= MAX_H3_RES; r += 1)
                    if (GetDirectionForResolution(r) != Direction.Invalid) return false;

                return true;
            }
        }

        /// <summary>
        /// The leading non-zero Direction "digit" of the index.
        /// </summary>
        public Direction LeadingNonZeroDirection {
            get {
                int resolution = Resolution;
                for (int r = 1; r <= resolution; r += 1) {
                    var idx = GetDirectionForResolution(r);
                    if (idx != Direction.Center) {
                        return idx;
                    }
                }

                return Direction.Center;
            }
        }

        /// <summary>
        /// Whether or not this index should be considered as a pentagon.
        /// </summary>
        public bool IsPentagon => BaseCell.IsPentagon &&
            LeadingNonZeroDirection == Direction.Center;

        /// <summary>
        /// The maximum number of possible icosahedron faces the index
        /// may intersect.
        /// </summary>
        public int MaximumFaceCount => IsPentagon ? 5 : 2;

        #endregion properties

        public H3Index() {
            Value = H3_INIT;
        }

        public H3Index(ulong value) {
            Value = value;
        }

        public H3Index(string value) {
            if (ulong.TryParse(value, NumberStyles.HexNumber, null, out ulong parsed)) Value = parsed;
        }

        public static H3Index Create(int resolution, int baseCell, Direction direction) {
            H3Index index = new() {
                Mode = Mode.Hexagon,
                Resolution = resolution,
                Direction = direction,
                BaseCellNumber = baseCell
            };

            for (int r = 1; r <= resolution; r += 1) index.SetDirectionForResolution(r, direction);

            return index;
        }

        #region manipulations

        /// <summary>
        /// Gets the Direction "digit" for the index at the specified resolution.
        /// </summary>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public Direction GetDirectionForResolution(int resolution) {
            var v = (int)((Value >> ((MAX_H3_RES - resolution) * H3_PER_DIGIT_OFFSET)) & H3_DIGIT_MASK);
            return (Direction)v;
        }

        /// <summary>
        /// Sets the Direction "digit" for the index at the specified resolution
        /// to the specifiied value.
        /// </summary>
        /// <param name="resolution"></param>
        /// <param name="direction"></param>
        public void SetDirectionForResolution(int resolution, Direction direction) {
            int offset = (MAX_H3_RES - resolution) * H3_PER_DIGIT_OFFSET;
            Value = (Value & ~(H3_DIGIT_MASK << (offset))) |
                (((ulong)direction) << (offset));
        }

        /// <summary>
        /// Increments the Direction "digit" for the index at the specified resolution.
        /// </summary>
        /// <param name="resolution"></param>
        public void IncrementDirectionForResolution(int resolution) {
            ulong val = 1UL;
            val <<= 3 * (15 - resolution);
            Value += val;
        }

        /// <summary>
        /// Zeros the Direction "digits" for the indexes starting at startResolution
        /// and ending at endResolution.
        /// </summary>
        /// <param name="startResolution"></param>
        /// <param name="endResolution"></param>
        public void ZeroDirectionsForResolutionRange(int startResolution, int endResolution) {
            if (startResolution > endResolution) return;

            ulong m = ~0UL;
            m <<= 3 * (endResolution - startResolution + 1);
            m = ~m;
            m <<= 3 * (15 - endResolution);
            m = ~m;

            Value &= m;
        }

        /// <summary>
        /// Rotates the index in place; skips any leading 1 digits (k-axis)
        /// </summary>
        /// <param name="rotateIndex">Callback to be fired in order to actually perform
        /// index rotation (eg clockwise or counter-clockwise)</param>
        /// <param name="rotateCell">Callback to be fired in order to actually perform
        /// direction digit rotation around the cell (eg clockwise or counter-clockwise)
        /// </param>
        private void RotatePentagon(Action rotateIndex, Func<Direction, Direction> rotateCell) {
            int resolution = Resolution;
            bool foundFirstNonZeroDigit = false;

            for (int r = 1; r <= resolution; r += 1) {
                // rotate digit
                SetDirectionForResolution(r, rotateCell(GetDirectionForResolution(r)));

                // look for the first non-zero digit so we
                // can adjust for deleted k-axes sequence
                // if necessary
                if (!foundFirstNonZeroDigit && GetDirectionForResolution(r) != Direction.Center) {
                    foundFirstNonZeroDigit = true;

                    // adjust for deleted k-axes sequence
                    if (LeadingNonZeroDirection == Direction.K) {
                        rotateIndex();
                    }
                }
            }
        }

        /// <summary>
        /// Performs an in-place 60 degree counter-clockwise pentagonal rotation of the index.
        /// </summary>
        public void RotatePentagonCounterClockwise() =>
            RotatePentagon(RotateCounterClockwise, cell => cell.RotateCounterClockwise());

        /// <summary>
        /// Performs an in-place 60 degree clockwise pentagonal rotation of the index.
        /// </summary>
        public void RotatePentagonClockwise() =>
            RotatePentagon(RotateClockwise, cell => cell.RotateClockwise());

        /// <summary>
        /// Performs an in-place 60 degree counter-clockwise rotation of the index.
        /// </summary>
        public void RotateCounterClockwise() {
            // rotate in place
            int resolution = Resolution;
            for (int r = 1; r <= resolution; r += 1)
                SetDirectionForResolution(r, GetDirectionForResolution(r).RotateCounterClockwise());
        }

        /// <summary>
        /// Performs an in-place 60 degree clockwise rotation of the index.
        /// </summary>
        public void RotateClockwise() {
            // rotate in place
            int resolution = Resolution;
            for (int r = 1; r <= resolution; r += 1)
                SetDirectionForResolution(r, GetDirectionForResolution(r).RotateClockwise());
        }

        #endregion manipulations

        #region conversions

        /// <summary>
        /// Convert an H3Index to the FaceIJK address on a specified icosahedral face.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public (FaceIJK, bool) ToFaceWithInitializedFijk(FaceIJK inputFaceIjk) {
            FaceIJK faceIjk = new(inputFaceIjk);
            int resolution = Resolution;

            // center base cell hierarchy is entirely on this face
            bool possibleOverage = true;
            if (!BaseCell.IsPentagon && (resolution == 0 || (faceIjk.Coord.I == 0 && faceIjk.Coord.J == 0 && faceIjk.Coord.K == 0))) {
                possibleOverage = false;
            }

            for (int r = 1; r <= resolution; r += 1) {
                if (IsResolutionClass3(r)) {
                    faceIjk.Coord.DownAperature7CounterClockwise();
                } else {
                    faceIjk.Coord.DownAperature7Clockwise();
                }

                faceIjk.Coord.ToNeighbour(GetDirectionForResolution(r));
            }

            return (faceIjk, possibleOverage);
        }

        /// <summary>
        /// Convert an H3Index to a FaceIJK address.
        /// </summary>
        /// <returns></returns>
        public FaceIJK ToFaceIJK() {
            H3Index index = new(this);

            if (BaseCell.IsPentagon && LeadingNonZeroDirection == Direction.IK) {
                index.RotateClockwise();
            }

            // start with the "home" face and ijk+ coordinates for the base cell of c
            var (fijk, overage) = index.ToFaceWithInitializedFijk(BaseCell.Home);

            // no overage is possible; h lies on this face
            if (!overage) return fijk;

            // if we're here we have the potential for an "overage"; i.e., it is
            // possible that c lies on an adjacent face
            CoordIJK origIJK = new(fijk.Coord);

            // if we're in Class III, drop into the next finer Class II grid
            int resolution = index.Resolution;
            if (IsResolutionClass3(resolution)) {
                fijk.Coord.DownAperature7Clockwise();
                resolution++;
            }

            // adjust for overage if needed
            // a pentagon base cell with a leading 4 digit requires special handling
            bool pentLeading4 = BaseCell.IsPentagon && index.LeadingNonZeroDirection == Direction.I;
            if (fijk.AdjustOverageClass2(resolution, pentLeading4, false) != Overage.None) {
                // if the base cell is a pentagon we have the potential for secondary
                // overages
                if (BaseCell.IsPentagon) {
                    while (fijk.AdjustOverageClass2(resolution, false, false) != Overage.None)
                        continue;
                }

                if (resolution != Resolution) {
                    fijk.Coord.UpAperature7Clockwise();
                }
            } else if (resolution != Resolution) {
                fijk.Coord = origIJK;
            }

            return fijk;
        }

        /// <summary>
        /// Determines the spherical coordinates of the center point of a H3
        /// index.
        /// </summary>
        /// <returns>Center point GeoCoord</returns>
        public GeoCoord ToGeoCoord() => ToFaceIJK().ToGeoCoord(Resolution);

        /// <summary>
        /// Determines the spherical coordinates of the center point of a H3
        /// index, and returns it as a NTS Point.
        /// </summary>
        /// <param name="geometryFactory">GeometryFactory to be used to create
        /// point; defaults to DefaultGeometryFactory.  Note that coordinates
        /// are provided in degrees and SRS is assumed to be EPSG:4326.</param>
        /// <returns></returns>
        public IPoint ToPoint(GeometryFactory? geometryFactory = null) =>
            ToGeoCoord().ToPoint(geometryFactory);

        /// <summary>
        /// Convert an FaceIJK address to the corresponding H3Index.
        /// </summary>
        /// <param name="face">The FaceIJK address</param>
        /// <param name="resolution">The cell resolution</param>
        /// <returns></returns>
        public static H3Index FromFaceIJK(FaceIJK face, int resolution) {
            if (resolution < 0 || resolution > MAX_H3_RES) return Invalid;

            H3Index index = new() {
                Mode = Mode.Hexagon,
                Resolution = resolution
            };

            if (resolution == 0) {
                if (face.BaseCellRotation == null) return Invalid;
                index.BaseCellNumber = face.BaseCellRotation.Cell;
                return index;
            }

            // we need to find the correct base cell FaceIJK for this H3 index;
            // start with the passed in face and resolution res ijk coordinates
            // in that face's coordinate system
            FaceIJK ijk = new(face);

            // build the H3Index from finest res up
            // adjust r for the fact that the res 0 base cell offsets the indexing
            // digits
            CoordIJK diff = new();
            CoordIJK last = new();
            CoordIJK lastCenter = new();
            for (int r = resolution - 1; r >= 0; r--) {
                last.I = ijk.Coord.I;
                last.J = ijk.Coord.J;
                last.K = ijk.Coord.K;

                if (IsResolutionClass3(r + 1)) {
                    // rotate ccw
                    ijk.Coord.UpAperature7CounterClockwise();
                    lastCenter.I = ijk.Coord.I;
                    lastCenter.J = ijk.Coord.J;
                    lastCenter.K = ijk.Coord.K;
                    lastCenter.DownAperature7CounterClockwise();
                } else {
                    // rotate cw
                    ijk.Coord.UpAperature7Clockwise();
                    lastCenter.I = ijk.Coord.I;
                    lastCenter.J = ijk.Coord.J;
                    lastCenter.K = ijk.Coord.K;
                    lastCenter.DownAperature7Clockwise();
                }

                diff.I = last.I - lastCenter.I;
                diff.J = last.J - lastCenter.J;
                diff.K = last.K - lastCenter.K;
                index.SetDirectionForResolution(r + 1, diff);
            }

            if (ijk.BaseCellRotation == null) {
                return Invalid;
            }

            // found our base cell
            index.BaseCellNumber = ijk.BaseCellRotation.Cell;
            var baseCell = ijk.BaseCellRotation.BaseCell;
            int numRotations = ijk.BaseCellRotation.CounterClockwiseRotations;

            // rotate if necessary to get canonical base cell orientation
            // for this base cell
            if (baseCell.IsPentagon) {
                // force rotation out of missing k-axes sub-sequence
                if (index.LeadingNonZeroDirection == Direction.K) {
                    // check for a cw/ ccw offset face; default is ccw
                    if (baseCell.FaceMatchesOffset(ijk.Face)) {
                        index.RotateClockwise();
                    } else {
                        index.RotateCounterClockwise();
                    }
                }

                for (int i = 0; i < numRotations; i += 1) {
                    index.RotatePentagonCounterClockwise();
                }
            } else {
                for (int i = 0; i < numRotations; i += 1) {
                    index.RotateCounterClockwise();
                }
            }

            return index;
        }

        /// <summary>
        /// Encodes a coordinate on the sphere to the H3 index of the containing cell at
        /// the specified resolution.
        /// </summary>
        /// <param name="geoCoord">The spherical coordinates to encode</param>
        /// <param name="resolution">The desired H3 resolution for the encoding</param>
        /// <returns>Returns H3Index.Invalid (H3_NULL) on invalid input</returns>
        public static H3Index FromGeoCoord(GeoCoord geoCoord, int resolution) {
            if (resolution < 0 || resolution > MAX_H3_RES) return Invalid;

            if (!geoCoord.Latitude.IsFinite() || !geoCoord.Longitude.IsFinite()) return Invalid;

            return FromFaceIJK(FaceIJK.FromGeoCoord(geoCoord, resolution), resolution);
        }

        public static H3Index FromPoint(IPoint point, int resolution) =>
            FromGeoCoord(GeoCoord.FromPoint(point), resolution);

        public static H3Index FromCoordinate(Coordinate coordinate, int resolution) =>
            FromGeoCoord(GeoCoord.FromPoint(new Point(coordinate.X, coordinate.Y)), resolution);

        public static implicit operator ulong(H3Index index) => index.Value;

        public static implicit operator H3Index(ulong value) => new H3Index(value);

        public override string ToString() => $"{Value:x}".ToLowerInvariant();

        #endregion conversions

        public int CompareTo(H3Index? other) {
            if (other == null) return 1;
            return Value.CompareTo(other.Value);
        }

        public static bool operator ==(H3Index? a, H3Index? b) => a?.Value == b?.Value;

        public static bool operator !=(H3Index? a, H3Index? b) => a?.Value != b?.Value;

        public static bool operator ==(H3Index? a, ulong b) => a?.Value == b;

        public static bool operator !=(H3Index? a, ulong b) => a?.Value != b;

        public override bool Equals(object? other) => (other is H3Index i && Value == i.Value) ||
            (other is ulong l && Value == l);

        public override int GetHashCode() => Value.GetHashCode();

    }

}
