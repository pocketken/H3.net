using System;
using System.Collections.Generic;
using System.Globalization;
using H3.Model;
using NetTopologySuite.Geometries;
using static H3.Constants;
using static H3.Utils;

#nullable enable

namespace H3 {

    public class H3Index {
        #region constants
        public static readonly H3Index Invalid = new H3Index(0);

        private const int H3_NUM_BITS = 64;
        private const int H3_MAX_OFFSET = 63;
        private const int H3_MODE_OFFSET = 59;
        private const int H3_BC_OFFSET = 45;
        private const int H3_RES_OFFSET = 52;
        private const int H3_RESERVED_OFFSET = 56;
        private const int H3_PER_DIGIT_OFFSET = 3;
        private const ulong H3_HIGH_BIT_MASK = (ulong)1 << H3_MAX_OFFSET;
        private const ulong H3_HIGH_BIT_MASK_NEGATIVE = ~H3_HIGH_BIT_MASK;
        private const ulong H3_MODE_MASK = (ulong)15 << H3_MODE_OFFSET;
        private const ulong H3_MODE_MASK_NEGATIVE = ~H3_MODE_MASK;
        private const ulong H3_BC_MASK = (ulong)127 << H3_BC_OFFSET;
        private const ulong H3_BC_MASK_NEGATIVE = ~H3_BC_MASK;
        private const ulong H3_RES_MASK = (ulong)15 << H3_RES_OFFSET;
        private const ulong H3_RES_MASK_NEGATIVE = ~H3_RES_MASK;
        private const ulong H3_RESERVED_MASK = (ulong)7 << H3_RESERVED_OFFSET;
        private const ulong H3_RESERVED_MASK_NEGATIVE = ~H3_RESERVED_MASK;
        private const ulong H3_DIGIT_MASK = 7;
        private const ulong H3_DIGIT_MASK_NEGATIVE = ~H3_DIGIT_MASK;

        /**
         * H3 index with mode 0, res 0, base cell 0, and 7 for all index digits.
         * Typically used to initialize the creation of an H3 cell index, which
         * expects all direction digits to be 7 beyond the cell's resolution.
        */
        private const ulong H3_INIT = 35184372088831UL;
        #endregion constants

        #region properties

        private ulong Value { get; set; } = 0;

        public BaseCell? BaseCell => IsValid ? LookupTables.BaseCells[BaseCellNumber] : null;

        public int HighBit {
            get => (int)((Value & H3_HIGH_BIT_MASK) >> H3_MAX_OFFSET);
            set => Value = (Value & H3_HIGH_BIT_MASK_NEGATIVE) | ((ulong)value << H3_MAX_OFFSET);
        }

        public Mode Mode {
            get => (Mode)((Value & H3_MODE_MASK) >> H3_MODE_OFFSET);
            set => Value = (Value & H3_MODE_MASK_NEGATIVE) | ((ulong)value << H3_MODE_OFFSET);
        }

        public int BaseCellNumber {
            get => (int)((Value & H3_BC_MASK) >> H3_BC_OFFSET);
            set => Value = (Value & H3_BC_MASK_NEGATIVE) | ((ulong)value << H3_BC_OFFSET);
        }

        public int Resolution {
            get => (int)((Value & H3_RES_MASK) >> H3_RES_OFFSET);
            set => Value = (Value & H3_RES_MASK_NEGATIVE) | ((ulong)value << H3_RES_OFFSET);
        }

        public CellIndex CellIndex {
            get => GetCellIndexForResolution(Resolution);
            set => SetCellIndexForResolution(Resolution, value);
        }

        public int ReservedBits {
            get => (int)((Value & H3_RESERVED_MASK) >> H3_RESERVED_OFFSET);
            set => Value = (Value & H3_RESERVED_MASK_NEGATIVE) | ((ulong)value << H3_RESERVED_OFFSET);
        }

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
                    CellIndex idx = GetCellIndexForResolution(r);

                    if (!foundFirstNonZeroDigit && idx != CellIndex.Center) {
                        foundFirstNonZeroDigit = true;
                        if (!LookupTables.BaseCells[baseCell].IsPentagon && idx == CellIndex.K) {
                            return false;
                        }
                    }

                    if (idx < CellIndex.Center || idx >= CellIndex.Invalid) {
                        return false;
                    }
                }

                for (int r = resolution + 1; r <= MAX_H3_RES; r += 1)
                    if (GetCellIndexForResolution(r) != CellIndex.Invalid) return false;

                return true;
            }
        }

        public CellIndex LeadingNonZeroCellIndex {
            get {
                int resolution = Resolution;
                for (int r = 1; r <= resolution; r += 1) {
                    var idx = GetCellIndexForResolution(r);
                    if (idx != CellIndex.Center) return idx;
                }

                return CellIndex.Center;
            }
        }

        public bool IsPentagon => LookupTables.BaseCells[BaseCellNumber].IsPentagon &&
            LeadingNonZeroCellIndex != CellIndex.Center;

        #endregion properties

        private H3Index() { }

        public H3Index(ulong value) {
            Value = value;
        }

        public H3Index(string value) {
            if (ulong.TryParse(value, NumberStyles.HexNumber, null, out ulong parsed)) Value = parsed;
        }

        #region hierarchy

        public H3Index GetParentForResolution(int parentResolution) {
            int resolution = Resolution;

            // ask for an invalid resolution or resolution greater than ours?
            if (parentResolution < 0 || parentResolution > MAX_H3_RES || parentResolution > resolution) return Invalid;

            // if its the same resolution, then we are our father.  err. yeah.
            if (resolution == parentResolution) return this;

            // return the parent index
            H3Index parentIndex = new H3Index(this) {
                Resolution = parentResolution
            };

            for (int r = parentResolution + 1; r <= resolution; r += 1)
                parentIndex.SetCellIndexForResolution(r, CellIndex.Invalid);

            return parentIndex;
        }

        public H3Index GetDirectChild(CellIndex cellIndex) => new H3Index(this) {
            Resolution = Resolution + 1,
            CellIndex = cellIndex
        };

        public H3Index GetChildCenterForResolution(int childResolution) {
            int resolution = Resolution;
            if (!IsValidChildResolution(resolution, childResolution)) return Invalid;
            if (resolution == childResolution) return this;

            H3Index childIndex = new H3Index(this) {
                Resolution = childResolution
            };

            for (int r = resolution + 1; r <= childResolution; r += 1) {
                childIndex.SetCellIndexForResolution(r, CellIndex.Center);
            }

            return childIndex;
        }

        public long GetMaxChildrenSizeForResolution(int childResolution) {
            int parentResolution = Resolution;
            if (!IsValidChildResolution(parentResolution, childResolution)) return 0;
            // TODO this is changing upstream to be pentago aware; port changes assuming we
            //      need this method at all.  @see https://github.com/uber/h3/issues/412
            return IPow(7, childResolution - parentResolution);
        }

        public IEnumerable<H3Index> GetChildrenAtResolution(int childResolution) {
            List<H3Index> children = new();
            int resolution = Resolution;

            if (!IsValidChildResolution(resolution, childResolution)) {
                return children;
            }

            if (resolution == childResolution) {
                children.Add(this);
                return children;
            }

            bool pentagon = IsPentagon;
            for (CellIndex i = 0; i < CellIndex.Invalid; i += 1) {
                if (pentagon && i == CellIndex.K) continue;
                children.AddRange(GetDirectChild(i).GetChildrenAtResolution(childResolution));
            }

            return children;
        }

        #endregion hierarchy

        #region manipulations

        public CellIndex GetCellIndexForResolution(int resolution) {
            var v = (int)((Value >> ((MAX_H3_RES - resolution) * H3_PER_DIGIT_OFFSET)) & H3_DIGIT_MASK);
            return (CellIndex)v;
        }

        public void SetCellIndexForResolution(int resolution, CellIndex cellIndex) {
            Value = (Value & ~(H3_DIGIT_MASK << ((MAX_H3_RES - resolution) * H3_PER_DIGIT_OFFSET))) |
                (((ulong)cellIndex) << ((MAX_H3_RES - resolution) * H3_PER_DIGIT_OFFSET));
        }

        private void RotatePentagon(Action rotateIndex, Func<CellIndex, CellIndex> rotateCell) {
            // rotate in place; skips any leading 1 digits (k-axis)

            int resolution = Resolution;
            bool foundFirstNonZeroDigit = false;

            for (int r = 1; r <= resolution; r += 1) {
                // rotate digit
                SetCellIndexForResolution(r, rotateCell(GetCellIndexForResolution(r)));

                // look for the first non-zero digit so we
                // can adjust for deleted k-axes sequence
                // if necessary
                if (!foundFirstNonZeroDigit && GetCellIndexForResolution(r) != CellIndex.Center) {
                    foundFirstNonZeroDigit = true;

                    // adjust for deleted k-axes sequence
                    if (LeadingNonZeroCellIndex == CellIndex.K) {
                        rotateIndex();
                    }
                }
            }
        }

        public void RotatePentagonCounterClockwise() =>
            RotatePentagon(RotateCounterClockwise, cell => cell.RotateCounterClockwise());

        public void RotatePentagoClockwise() =>
            RotatePentagon(RotateClockwise, cell => cell.RotateClockwise());

        public void RotateCounterClockwise() {
            // rotate in place
            int resolution = Resolution;
            for (int r = 1; r <= resolution; r += 1)
                SetCellIndexForResolution(r, GetCellIndexForResolution(r).RotateCounterClockwise());
        }

        public void RotateClockwise() {
            // rotate in place
            int resolution = Resolution;
            for (int r = 1; r <= resolution; r += 1)
                SetCellIndexForResolution(r, GetCellIndexForResolution(r).RotateClockwise());
        }

        #endregion manipulations

        #region conversions

        public static H3Index CreateIndex(int resolution, int baseCell, CellIndex cellIndex) {
            H3Index index = new H3Index(H3_INIT) {
                Mode = Mode.Hexagon,
                Resolution = resolution,
                BaseCellNumber = baseCell,
                CellIndex = cellIndex
            };

            for (int r = 1; r < resolution; r += 1) index.SetCellIndexForResolution(r, cellIndex);

            return index;
        }

        public static H3Index FromFaceIJK(FaceIJK face, int resolution) {
            if (resolution < 0 || resolution > MAX_H3_RES) return Invalid;

            H3Index index = new H3Index(H3_INIT) {
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
            FaceIJK ijk = new FaceIJK(face);

            // build the H3Index from finest res up
            // adjust r for the fact that the res 0 base cell offsets the indexing
            // digits
            for (int r = resolution - 1; r >= 0; r--) {
                CoordIJK last = new CoordIJK(ijk.Coord);
                CoordIJK lastCenter;

                if (IsResolutionClass3(r + 1)) {
                    // rotate ccw
                    ijk.Coord.UpAperature7CounterClockwise();
                    lastCenter = new CoordIJK(ijk.Coord).DownAperature7CounterClockwise();
                } else {
                    // rotate cw
                    ijk.Coord.UpAperature7Clockwise();
                    lastCenter = new CoordIJK(ijk.Coord).DownAperature7Clockwise();
                }

                CoordIJK diff = (last - lastCenter).Normalize();
                index.SetCellIndexForResolution(r + 1, diff);
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
                if (index.LeadingNonZeroCellIndex == CellIndex.K) {
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

        public static H3Index FromGeoCoord(GeoCoord geoCoord, int resolution) {
            if (resolution < 0 || resolution > MAX_H3_RES) return Invalid;

            if (!geoCoord.Latitude.IsFinite() || !geoCoord.Longitude.IsFinite()) return Invalid;

            return FromFaceIJK(FaceIJK.FromGeoCoord(geoCoord, resolution), resolution);
        }

        public static H3Index FromPoint(Point point, int resolution) =>
            FromGeoCoord(GeoCoord.FromPoint(point), resolution);

        public static implicit operator ulong(H3Index index) => index.Value;

        public static implicit operator H3Index(ulong value) => new H3Index(value);

        public override string ToString() => $"{Value:x}".ToLowerInvariant();

        #endregion conversions

        public override bool Equals(object? other) => (other is H3Index i && Value == i.Value) ||
            (other is ulong l && Value == l);

        public override int GetHashCode() => Value.GetHashCode();
    }

}
