using System;
using System.Globalization;
using H3.Model;
using static H3.Constants;
using static H3.Utils;
using NetTopologySuite.Geometries;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

#nullable enable

namespace H3 {

    [JsonConverter(typeof(H3IndexJsonConverter))]
    public sealed partial class H3Index : IComparable<H3Index> {

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

        // Number of bits in each of the resolution, base cell, and digit regions.
        private const int N_BITS_RES = 4;
        private const int N_BITS_BASE_CELL = 7;
        private const int N_BITS_DIGIT = H3_PER_DIGIT_OFFSET;

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

        internal ulong Value { get; set; }

        public BaseCell BaseCell {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BaseCells.Cells[BaseCellNumber];
        }

        /// <summary>
        /// The highest bit value of the index.
        /// </summary>
        public int HighBit {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((Value & H3_HIGH_BIT_MASK) >> H3_MAX_OFFSET);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Value = (Value & H3_HIGH_BIT_MASK_NEGATIVE) | ((ulong)value << H3_MAX_OFFSET);
        }

        /// <summary>
        /// The Mode of the index.
        /// </summary>
        public Mode Mode {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Mode)((Value & H3_MODE_MASK) >> H3_MODE_OFFSET);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Value = (Value & H3_MODE_MASK_NEGATIVE) | ((ulong)value << H3_MODE_OFFSET);
        }

        /// <summary>
        /// The base cell number of the index.  Must be &gt;= 0 &lt; NUM_BASE_CELLS
        /// </summary>
        public int BaseCellNumber {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((Value & H3_BC_MASK) >> H3_BC_OFFSET);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Value = (Value & H3_BC_MASK_NEGATIVE) | ((ulong)value << H3_BC_OFFSET);
        }

        /// <summary>
        /// The resolution of the index.  Must be &gt;= 0 &lt;= MAX_H3_RES
        /// </summary>
        public int Resolution {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((Value & H3_RES_MASK) >> H3_RES_OFFSET);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((Value & H3_RESERVED_MASK) >> H3_RESERVED_OFFSET);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Value = (Value & H3_RESERVED_MASK_NEGATIVE) | ((ulong)value << H3_RESERVED_OFFSET);
        }

        /// <summary>
        /// Whether or not the index is valid.
        /// </summary>
        public bool IsValid {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                // the 1 high bit should be 0b0
                // the 4 mode bits should be 0b0001 (H3_CELL_MODE)
                // the 3 reserved bits should be 0b000
                // in total, the top 8 bits should be 0b00001000
                if (Value.GetTopBits(8) != 0b00001000) return false;

                var value = Value << 8;

                // no need to check resolution; any 4-bit number is a valid resolution.
                var res = value.GetTopBits(N_BITS_RES);
                value <<= N_BITS_RES;

                // check that base cell number is valid.
                var bc = value.GetTopBits(N_BITS_BASE_CELL);
                if (bc >= NUM_BASE_CELLS) {
                    return false;
                }
                value <<= N_BITS_BASE_CELL;

                // now check that each resolution digit is valid.
                // let `r` denote the resolution we're currently checking.
                var r = 1UL;

                // Pentagon cells start with a sequence of 0's (CENTER_DIGIT's).
                // The first nonzero digit can't be a 1 (i.e., "deleted subsequence",
                // PENTAGON_SKIPPED_DIGIT, or K_AXES_DIGIT).
                // Test for pentagon base cell first to avoid this loop if possible.
                if (BaseCells.Cells[bc].IsPentagon) {
                    while (r <= res) {
                        var d = value.GetTopBits(N_BITS_DIGIT);
                        if (d == (ulong)Direction.Center) {
                            value <<= N_BITS_DIGIT;
                        } else if (d == (ulong)Direction.K) {
                            return false;
                        } else {
                            // But don't increment `r`, since we still need to
                            // check that it isn't INVALID_DIGIT.
                            break;
                        }
                        r++;
                    }
                }

                // After (possibly) taking care of pentagon logic, check that
                // the remaining digits up to `res` are not 7 (INVALID_DIGIT).
                while (r <= res) {
                    if (value.GetTopBits(N_BITS_DIGIT) == (ulong)Direction.Invalid) return false;
                    value <<= N_BITS_DIGIT;
                    r++;
                }

                // Now check that all the unused digits after `res` are
                // set to 7 (INVALID_DIGIT).
                var shift = (int)(15 - res) * 3;
                ulong mask = 0;
                mask = ~mask;
                mask >>= shift;
                mask = ~mask;
                return value == mask;
            }
        }

        /// <summary>
        /// The leading non-zero Direction "digit" of the index.
        /// </summary>
        public Direction LeadingNonZeroDirection {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                var resolution = Resolution;
                for (var r = 1; r <= resolution; r += 1) {
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
        public bool IsPentagon {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BaseCell.IsPentagon && LeadingNonZeroDirection == Direction.Center;
        }

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
            if (ulong.TryParse(value, NumberStyles.HexNumber, null, out var parsed)) Value = parsed;
        }

        public static H3Index Create(int resolution, int baseCell, Direction direction) {
            H3Index index = new() {
                Mode = Mode.Cell,
                Resolution = resolution,
                Direction = direction,
                BaseCellNumber = baseCell
            };

            for (var r = 1; r <= resolution; r += 1) index.SetDirectionForResolution(r, direction);

            return index;
        }

        #region manipulations

        /// <summary>
        /// Gets the Direction "digit" for the index at the specified resolution.
        /// </summary>
        /// <param name="resolution"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDirectionForResolution(int resolution, Direction direction) {
            var offset = (MAX_H3_RES - resolution) * H3_PER_DIGIT_OFFSET;
            Value = (Value & ~(H3_DIGIT_MASK << offset)) |
                ((ulong)direction << offset);
        }

        /// <summary>
        /// Increments the Direction "digit" for the index at the specified resolution.
        /// </summary>
        /// <param name="resolution"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void IncrementDirectionForResolution(int resolution) {
            var val = 1UL;
            val <<= H3_PER_DIGIT_OFFSET * (15 - resolution);
            Value += val;
        }

        /// <summary>
        /// Zeros the Direction "digits" for the indexes starting at startResolution
        /// and ending at endResolution.
        /// </summary>
        /// <param name="startResolution"></param>
        /// <param name="endResolution"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ZeroDirectionsForResolutionRange(int startResolution, int endResolution) {
            if (startResolution > endResolution) return;

            var m = ~0UL;
            m <<= H3_PER_DIGIT_OFFSET * (endResolution - startResolution + 1);
            m = ~m;
            m <<= H3_PER_DIGIT_OFFSET * (15 - endResolution);
            m = ~m;

            Value &= m;
        }

        /// <summary>
        /// Invalidates the Direction "digits" for the indexes starting at startResolution
        /// and ending at endResolution
        /// </summary>
        /// <param name="startResolution"></param>
        /// <param name="endResolution"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InvalidateDirectionsForResolutionRange(int startResolution, int endResolution) {
            if (startResolution > endResolution) return;

            var m = ~0UL;
            m <<= H3_PER_DIGIT_OFFSET * (endResolution - startResolution + 1);
            m = ~m;
            m <<= H3_PER_DIGIT_OFFSET * (15 - endResolution);

            Value |= m;
        }

        /// <summary>
        /// Performs an in-place 60 degree clockwise rotation of the index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateClockwise() => RotateClockwise(1);

        /// <summary>
        /// Performs an in-place 60 degree clockwise rotation of the index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateCounterClockwise() => RotateCounterClockwise(1);

        /// <summary>
        /// Performs an in-place 60 degree counter-clockwise pentagonal rotation of the index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotatePentagonCounterClockwise() {
            var resolution = Resolution;
            var foundFirstNonZeroDigit = false;

            for (var r = 1; r <= resolution; r += 1) {
                // rotate digit
                SetDirectionForResolution(r, GetDirectionForResolution(r).RotateCounterClockwise());

                // look for the first non-zero digit so we
                // can adjust for deleted k-axes sequence
                // if necessary
                if (foundFirstNonZeroDigit || GetDirectionForResolution(r) == Direction.Center)
                    continue;

                foundFirstNonZeroDigit = true;

                // adjust for deleted k-axes sequence
                if (LeadingNonZeroDirection == Direction.K) {
                    RotateCounterClockwise();
                }
            }
        }

        /// <summary>
        /// Performs an in-place 60 degree clockwise pentagonal rotation of the index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotatePentagonClockwise() {
            var resolution = Resolution;
            var foundFirstNonZeroDigit = false;

            for (var r = 1; r <= resolution; r += 1) {
                // rotate digit
                SetDirectionForResolution(r, GetDirectionForResolution(r).RotateClockwise());

                // look for the first non-zero digit so we
                // can adjust for deleted k-axes sequence
                // if necessary
                if (foundFirstNonZeroDigit || GetDirectionForResolution(r) == Direction.Center)
                    continue;

                foundFirstNonZeroDigit = true;

                // adjust for deleted k-axes sequence
                if (LeadingNonZeroDirection == Direction.K) {
                    RotateClockwise();
                }
            }
        }

        #endregion manipulations

        #region conversions

        /// <summary>
        /// Convert an H3Index to the FaceIJK address on a specified icosahedral face.  Note that
        /// <paramref name="faceIjk"/> will be mutated by this function.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool ToFaceWithInitializedFijk(FaceIJK faceIjk) {
            var resolution = Resolution;

            // center base cell hierarchy is entirely on this face
            var possibleOverage = !(!BaseCell.IsPentagon && (resolution == 0 || faceIjk.Coord.I == 0 && faceIjk.Coord.J == 0 && faceIjk.Coord.K == 0));

            for (var r = 1; r <= resolution; r += 1) {
                if (IsResolutionClass3(r)) {
                    faceIjk.Coord.DownAperture7CounterClockwise();
                } else {
                    faceIjk.Coord.DownAperture7Clockwise();
                }

                faceIjk.Coord.ToNeighbour(GetDirectionForResolution(r));
            }

            return possibleOverage;
        }

        /// <summary>
        /// Convert an H3Index to a FaceIJK address.
        /// </summary>
        /// <returns></returns>
        public FaceIJK ToFaceIJK(FaceIJK? toUpdateFijk = default) {
            H3Index index = this;

            if (BaseCell.IsPentagon && LeadingNonZeroDirection == Direction.IK) {
                index = new(this);
                index.RotateClockwise();
            }

            // start with the "home" face and ijk+ coordinates for the base cell of c
            var fijk = toUpdateFijk ?? new FaceIJK();
            fijk.Face = BaseCell.Home.Face;
            fijk.Coord.I = BaseCell.Home.Coord.I;
            fijk.Coord.J = BaseCell.Home.Coord.J;
            fijk.Coord.K = BaseCell.Home.Coord.K;
            var overage = index.ToFaceWithInitializedFijk(fijk);

            // no overage is possible; h lies on this face
            if (!overage) return fijk;

            // if we're here we have the potential for an "overage"; i.e., it is
            // possible that c lies on an adjacent face
            var pI = fijk.Coord.I;
            var pJ = fijk.Coord.J;
            var pK = fijk.Coord.K;

            // if we're in Class III, drop into the next finer Class II grid
            var currentResolution = Resolution;
            var resolution = currentResolution;
            if (IsResolutionClass3(resolution)) {
                fijk.Coord.DownAperture7Clockwise();
                resolution++;
            }

            // adjust for overage if needed
            // a pentagon base cell with a leading 4 digit requires special handling
            var pentLeading4 = BaseCell.IsPentagon && index.LeadingNonZeroDirection == Direction.I;
            if (fijk.AdjustOverageClass2(resolution, pentLeading4, false) != Overage.None) {
                // if the base cell is a pentagon we have the potential for secondary
                // overages
                if (BaseCell.IsPentagon) {
                    while (fijk.AdjustOverageClass2(resolution, false, false) != Overage.None) { }
                }

                if (resolution != currentResolution) {
                    fijk.Coord.UpAperture7Clockwise();
                }
            } else if (resolution != currentResolution) {
                fijk.Coord.I = pI;
                fijk.Coord.J = pJ;
                fijk.Coord.K = pK;
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
        public Point ToPoint(GeometryFactory? geometryFactory = null) =>
            ToGeoCoord().ToPoint(geometryFactory);

        /// <summary>
        /// Convert an FaceIJK address to the corresponding H3Index.
        /// </summary>
        /// <param name="face">The FaceIJK address</param>
        /// <param name="resolution">The cell resolution</param>
        /// <returns></returns>
        public static H3Index FromFaceIJK(FaceIJK face, int resolution) {
            if (resolution is < 0 or > MAX_H3_RES) return Invalid;

            H3Index index = new() {
                Mode = Mode.Cell,
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
            CoordIJK ijk = new(face.Coord);

            // build the H3Index from finest res up
            // adjust r for the fact that the res 0 base cell offsets the indexing
            // digits
            CoordIJK last = new();
            CoordIJK lastCenter = new();
            for (var r = resolution - 1; r >= 0; r--) {
                last.I = ijk.I;
                last.J = ijk.J;
                last.K = ijk.K;

                if (IsResolutionClass3(r + 1)) {
                    // rotate ccw
                    ijk.UpAperture7CounterClockwise();
                    lastCenter.I = ijk.I;
                    lastCenter.J = ijk.J;
                    lastCenter.K = ijk.K;
                    lastCenter.DownAperture7CounterClockwise();
                } else {
                    // rotate cw
                    ijk.UpAperture7Clockwise();
                    lastCenter.I = ijk.I;
                    lastCenter.J = ijk.J;
                    lastCenter.K = ijk.K;
                    lastCenter.DownAperture7Clockwise();
                }

                last.I -= lastCenter.I;
                last.J -= lastCenter.J;
                last.K -= lastCenter.K;
                index.SetDirectionForResolution(r + 1, last);
            }

            if (ijk.I > MAX_FACE_COORD || ijk.J > MAX_FACE_COORD || ijk.K > MAX_FACE_COORD) return Invalid;
            var baseCellRotation = LookupTables.FaceIjkBaseCells[face.Face, ijk.I, ijk.J, ijk.K];

            // found our base cell
            index.BaseCellNumber = baseCellRotation.Cell;
            var baseCell = baseCellRotation.BaseCell;
            var numRotations = baseCellRotation.CounterClockwiseRotations;

            // rotate if necessary to get canonical base cell orientation
            // for this base cell
            if (baseCell.IsPentagon) {
                // force rotation out of missing k-axes sub-sequence
                if (index.LeadingNonZeroDirection == Direction.K) {
                    // check for a cw/ ccw offset face; default is ccw
                    if (baseCell.FaceMatchesOffset(face.Face)) {
                        index.RotateClockwise();
                    } else {
                        index.RotateCounterClockwise();
                    }
                }

                for (var i = 0; i < numRotations; i += 1) {
                    index.RotatePentagonCounterClockwise();
                }
            } else {
                index.RotateCounterClockwise(numRotations);
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
            if (resolution is < 0 or > MAX_H3_RES) return Invalid;

            if (!geoCoord.Latitude.IsFinite() || !geoCoord.Longitude.IsFinite()) return Invalid;

            return FromFaceIJK(FaceIJK.FromGeoCoord(geoCoord.Longitude, geoCoord.Latitude, resolution), resolution);
        }

        public static H3Index FromPoint(Point point, int resolution) =>
            FromGeoCoord(GeoCoord.FromPoint(point), resolution);

        public static implicit operator ulong(H3Index index) => index.Value;

        public static implicit operator H3Index(ulong value) => new(value);

        public override string ToString() => $"{Value:x}".ToLowerInvariant();

        #endregion conversions

        public int CompareTo(H3Index? other) {
            return other == null ? 1 : Value.CompareTo(other.Value);
        }

        public static bool operator ==(H3Index? a, H3Index? b) {
            if (a is null) return b is null;
            if (b is null) return false;
            return a.Value == b.Value;
        }

        public static bool operator !=(H3Index? a, H3Index? b) {
            if (a is null) return b is not null;
            if (b is null) return true;
            return a.Value != b.Value;
        }

        public static bool operator ==(H3Index? a, ulong b) {
            if (a is null) return false;
            return a.Value == b;
        }

        public static bool operator !=(H3Index? a, ulong b) {
            if (a is null) return true;
            return a.Value != b;
        }

        public override bool Equals(object? other) => other is H3Index i && Value == i.Value ||
            other is ulong l && Value == l;

        public override int GetHashCode() => Value.GetHashCode();

    }

}
