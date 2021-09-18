using System;
using H3.Model;
using static H3.Utils;

#nullable enable

namespace H3.Extensions {

    /// <summary>
    /// Extends the H3Index class with support for generating LocalIJ coordinates.
    ///
    /// This functionality is experimental, and its output is not guaranteed
    /// to be compatible across different versions of H3.
    /// </summary>
    public static class H3LocalIJExtensions {

        /// <summary>
        /// Produces ij coordinates for an index anchored by an origin.
        ///
        /// The coordinate space used by this function may have deleted
        /// regions or warping due to pentagonal distortion.
        ///
        /// Coordinates are only comparable if they come from the same
        /// origin index.
        ///
        /// Failure may occur if the index is too far away from the origin
        /// or if the index is on the other side of a pentagon.
        ///
        /// This function is experimental, and its output is not guaranteed
        /// to be compatible across different versions of H3.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static CoordIJ ToLocalIJ(this H3Index origin, H3Index index) =>
            CoordIJ.FromCoordIJK(LocalCoordIJK.ToLocalIJK(origin, index));

        /// <summary>
        /// Produces an index for ij coordinates anchored by an origin.
        ///
        /// The coordinate space used by this function may have deleted
        /// regions or warping due to pentagonal distortion.
        ///
        /// Failure may occur if the index is too far away from the origin
        /// or if the index is on the other side of a pentagon.
        ///
        /// This function is experimental, and its output is not guaranteed
        /// to be compatible across different versions of H3.
        /// </summary>
        /// <param name="origin">an anchoring index for the IJ coordinate system</param>
        /// <param name="coord">IJ coordinates to index</param>
        /// <returns>H3Index for coordintes</returns>
        public static H3Index FromLocalIJ(this H3Index origin, CoordIJ coord) {
            try {
                return LocalCoordIJK.ToH3Index(origin, coord.ToCoordIJK());
            } catch {
                return H3Index.Invalid;
            }
        }

    }

    /// <summary>
    /// Extends the H3Index class with support for generating LocalIJK coordinates.
    ///
    /// This functionality is experimental, and its output is not guaranteed
    /// to be compatible across different versions of H3.
    /// </summary>
    public static class H3LocalIJKExtensions {

        /// <summary>
        /// Produces ijk coordinates for an index anchored by an origin.
        ///
        /// The coordinate space used by this function may have deleted
        /// regions or warping due to pentagonal distortion.
        ///
        /// Coordinates are only comparable if they come from the same
        /// origin index.
        ///
        /// Failure may occur if the index is too far away from the origin
        /// or if the index is on the other side of a pentagon.
        ///
        /// This function is experimental, and its output is not guaranteed
        /// to be compatible across different versions of H3.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static CoordIJK ToLocalIJK(this H3Index origin, H3Index index) =>
            LocalCoordIJK.ToLocalIJK(origin, index);

        /// <summary>
        /// Produces an index for ijk coordinates anchored by an origin.
        ///
        /// The coordinate space used by this function may have deleted
        /// regions or warping due to pentagonal distortion.
        ///
        /// Failure may occur if the index is too far away from the origin
        /// or if the index is on the other side of a pentagon.
        ///
        /// This function is experimental, and its output is not guaranteed
        /// to be compatible across different versions of H3.
        /// </summary>
        /// <param name="origin">an anchoring index for the IJK coordinate system</param>
        /// <param name="coord">IJK coordinates to index</param>
        /// <returns>H3Index for coordintes</returns>
        public static H3Index FromLocalIJK(this H3Index origin, CoordIJK coord) {
            try {
                return LocalCoordIJK.ToH3Index(origin, coord);
            }  catch {
                return H3Index.Invalid;
            }
        }

    }

    public static class LocalCoordIJK {

        /// <summary>
        /// Produces ijk+ coordinates for an index anchored by an origin.
        ///
        /// The coordinate space used by this function may have deleted
        /// regions or warping due to pentagonal distortion.
        ///
        /// Coordinates are only comparable if they come from the same
        /// origin index.
        ///
        /// Failure may occur if the index is too far away from the origin
        /// or if the index is on the other side of a pentagon.
        /// </summary>
        /// <param name="origin">an anchoring index for the IJ coordinate system</param>
        /// <param name="index">index to generate IJ coordinates for</param>
        /// <returns>local IJ coordinates</returns>
        public static CoordIJK ToLocalIJK(H3Index origin, H3Index destination) {
            H3Index index = new(destination);
            int resolution = origin.Resolution;
            if (resolution != index.Resolution) {
                throw new ArgumentOutOfRangeException(nameof(index), "must be same resolution as origin");
            }

            BaseCell originBaseCell = origin.BaseCell;
            BaseCell baseCell = index.BaseCell;

            // Direction from origin base cell to index base cell
            Direction dir = Direction.Center;
            Direction revDir = Direction.Center;

            if (originBaseCell != baseCell) {
                dir = BaseCell.GetNeighbourDirection(originBaseCell.Cell, baseCell.Cell);
                if (dir == Direction.Invalid) {
                    throw new ArgumentException($"index cell {baseCell.Cell} is not a neighbour of origin cell {originBaseCell.Cell}");
                }

                revDir = BaseCell.GetNeighbourDirection(baseCell.Cell, originBaseCell.Cell);
            }

            bool originOnPent = originBaseCell.IsPentagon;
            bool indexOnPent = baseCell.IsPentagon;

            if (dir != Direction.Center) {
                // Rotate index into the orientation of the origin base cell.
                // cw because we are undoing the rotation into that base cell.
                int baseCellRotations = LookupTables.NeighbourCounterClockwiseRotations[originBaseCell.Cell, (int)dir];
                if (indexOnPent) {
                    for (int i = 0; i < baseCellRotations; i += 1) {
                        index.RotatePentagonClockwise();
                        revDir = revDir.RotateClockwise();
                        if (revDir == Direction.K) revDir = revDir.RotateClockwise();
                    }
                } else {
                    for (int i = 0; i < baseCellRotations; i += 1) {
                        index.RotateClockwise();
                        revDir = revDir.RotateClockwise();
                    }
                }
            }

            var (indexFijk, _) = index.ToFaceWithInitializedFijk(new FaceIJK());
            if (dir != Direction.Center) {
                if (originBaseCell == baseCell) throw new Exception("assertion failed; origin should not equal index cell");
                if (originOnPent && indexOnPent) throw new Exception("assertion failed; origin and index cannot both be on a pentagon");

                int pentagonRotations = 0;
                int directionRotations = 0;

                if (originOnPent) {
                    Direction originLeadingDigit = origin.LeadingNonZeroDirection;
                    if (LookupTables.UnfoldableDirections[(int)originLeadingDigit, (int)dir]) {
                        // TODO: We may be unfolding the pentagon incorrectly in this
                        // case; return an error code until this is guaranteed to be
                        // correct.
                        throw new Exception("origin -> dir results in unfoldable pentagon");
                    }

                    directionRotations = LookupTables.PentagonRotations[(int)originLeadingDigit, (int)dir];
                    pentagonRotations = directionRotations;
                } else if (indexOnPent) {
                    Direction indexLeadingDigit = index.LeadingNonZeroDirection;
                    if (LookupTables.UnfoldableDirections[(int)indexLeadingDigit, (int)revDir]) {
                        // TODO: We may be unfolding the pentagon incorrectly in this
                        // case; return an error code until this is guaranteed to be
                        // correct.
                        throw new Exception("index -> revDir results in unfoldable pentagon");
                    }

                    pentagonRotations = LookupTables.PentagonRotations[(int)revDir, (int)indexLeadingDigit];
                }

                if (pentagonRotations < 0) throw new Exception("no pentagon rotations");
                if (directionRotations < 0) throw new Exception("no direction rotations");

                for (int i = 0; i < pentagonRotations; i += 1) indexFijk.Coord.RotateClockwise();

                CoordIJK offset = new CoordIJK(0, 0, 0).ToNeighbour(dir);
                // scale offset based upon resolution
                for (int r = resolution - 1; r >= 0; r -= 1) {
                    if (IsResolutionClass3(r + 1)) {
                        offset.DownAperature7CounterClockwise();
                    } else {
                        offset.DownAperature7Clockwise();
                    }
                }

                for (int i = 0; i < directionRotations; i += 1) offset.RotateClockwise();

                // perform necesary translation
                indexFijk.Coord.I += offset.I;
                indexFijk.Coord.J += offset.J;
                indexFijk.Coord.K += offset.K;
                indexFijk.Coord.Normalize();
            } else if (originOnPent && indexOnPent) {
                // If the origin and index are on pentagon, and we checked that the base
                // cells are the same or neighboring, then they must be the same base
                // cell.
                if (originBaseCell != baseCell) throw new Exception("origin and index base cells must equal");

                Direction originLeadingDigit = origin.LeadingNonZeroDirection;
                Direction indexLeadingDigit = index.LeadingNonZeroDirection;

                if (LookupTables.UnfoldableDirections[(int)originLeadingDigit, (int)indexLeadingDigit]) {
                    // TODO: We may be unfolding the pentagon incorrectly in this case;
                    // return an error code until this is guaranteed to be correct.
                    throw new Exception("origin -> index results in unfoldable pentagon");
                }

                int withinPentagonRotations = LookupTables.PentagonRotations[(int)originLeadingDigit, (int)indexLeadingDigit];

                for (int i = 0; i < withinPentagonRotations; i += 1) indexFijk.Coord.RotateClockwise();
            }

            return indexFijk.Coord;
        }

        /// <summary>
        /// Produces an index for ijk+ coordinates anchored by an origin.
        ///
        /// The coordinate space used by this function may have deleted
        /// regions or warping due to pentagonal distortion.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="ijk"></param>
        /// <returns></returns>
        public static H3Index ToH3Index(H3Index origin, CoordIJK ijk) {
            int resolution = origin.Resolution;
            BaseCell? originBaseCell = origin.BaseCell;
            if (originBaseCell == null) throw new Exception("origin is not a valid base cell");
            bool originOnPent = originBaseCell.IsPentagon;

            H3Index index = new() {
                Mode = Mode.Cell,
                Resolution = resolution
            };

            if (resolution == 0) {
                if (ijk.I > 1 || ijk.J > 1 || ijk.K > 1) throw new Exception("input coordinates out of range");

                int newBaseCell = LookupTables.Neighbours[originBaseCell.Cell, (int)(Direction)ijk];
                if (newBaseCell == LookupTables.INVALID_BASE_CELL) throw new Exception("moved in invalid direction off pentagon");

                index.BaseCellNumber = newBaseCell;
                return index;
            }

            // we need to find the correct base cell offset (if any) for this H3 index;
            // start with the passed in base cell and resolution res ijk coordinates
            // in that base cell's coordinate system
            CoordIJK ijkCopy = new(ijk);

            // build the H3Index from finest res up
            // adjust r for the fact that the res 0 base cell offsets the indexing
            // digits
            CoordIJK lastIJK = new();
            CoordIJK lastCenter = new();
            CoordIJK diff = new();
            for (int r = resolution - 1; r >= 0; r -= 1) {
                lastIJK.I = ijkCopy.I;
                lastIJK.J = ijkCopy.J;
                lastIJK.K = ijkCopy.K;

                if (IsResolutionClass3(r + 1)) {
                    // rotate ccw
                    ijkCopy.UpAperature7CounterClockwise();
                    lastCenter.I = ijkCopy.I;
                    lastCenter.J = ijkCopy.J;
                    lastCenter.K = ijkCopy.K;
                    lastCenter.DownAperature7CounterClockwise();
                } else {
                    // rotate cw
                    ijkCopy.UpAperature7Clockwise();
                    lastCenter.I = ijkCopy.I;
                    lastCenter.J = ijkCopy.J;
                    lastCenter.K = ijkCopy.K;
                    lastCenter.DownAperature7Clockwise();
                }

                diff.I = lastIJK.I - lastCenter.I;
                diff.J = lastIJK.J - lastCenter.J;
                diff.K = lastIJK.K - lastCenter.K;
                index.SetDirectionForResolution(r + 1, diff);
            }

            // ijkCopy should now hold the IJK of the base cell in the
            // coordinate system of the current base cell
            if (ijkCopy.I > 1 || ijkCopy.J > 1 || ijkCopy.K > 1) throw new Exception("input is out of range");

            // lookup correct base cell
            Direction dir = ijkCopy;
            BaseCell? baseCell = originBaseCell.Neighbour(dir);

            // If baseCell is invalid, it must be because the origin base cell is a
            // pentagon, and because pentagon base cells do not border each other,
            // baseCell must not be a pentagon.
            bool indexOnPent = baseCell != null && baseCell.IsPentagon;

            if (dir != Direction.Center) {
                // If the index is in a warped direction, we need to unwarp the base
                // cell direction. There may be further need to rotate the index digits.
                int pentagonRotations = 0;

                if (originOnPent) {
                    Direction originLeadingDigit = origin.LeadingNonZeroDirection;
                    pentagonRotations = LookupTables.PentagonRotationsInReverse[(int)originLeadingDigit, (int)dir];

                    for (int i = 0; i < pentagonRotations; i += 1) dir = dir.RotateCounterClockwise();

                    // The pentagon rotations are being chosen so that dir is not the
                    // deleted direction. If it still happens, it means we're moving
                    // into a deleted subsequence, so there is no index here.
                    if (dir == Direction.K) throw new Exception("cannot move into deleted subsequence");

                    baseCell = originBaseCell.Neighbour(dir);

                    // indexOnPent does not need to be checked again since no pentagon
                    // base cells border each other.
                    if (baseCell == null || baseCell.IsPentagon) {
                        throw new Exception("unable to translate coordinate to index");
                    }
                }

                // did we get a base cell?
                if (baseCell == null) throw new Exception("unable to determine initial base cell");

                // Now we can determine the relation between the origin and target base
                // cell.
                int baseCellRotations = LookupTables.NeighbourCounterClockwiseRotations[originBaseCell.Cell, (int)dir];
                if (baseCellRotations < 0) throw new Exception("invalid number of base cell rotations");

                // Adjust for pentagon warping within the base cell. The base cell
                // should be in the right location, so now we need to rotate the index
                // back. We might not need to check for errors since we would just be
                // double mapping.
                if (indexOnPent) {
                    Direction revDir = BaseCell.GetNeighbourDirection(baseCell.Cell, originBaseCell.Cell);
                    if (revDir == Direction.Invalid) throw new Exception("invalid rotation direction");

                    // Adjust for the different coordinate space in the two base cells.
                    // This is done first because we need to do the pentagon rotations
                    // based on the leading digit in the pentagon's coordinate system.
                    for (int i = 0; i < baseCellRotations; i += 1) index.RotateCounterClockwise();

                    Direction indexLeadingDigit = index.LeadingNonZeroDirection;
                    var table = baseCell.IsPolarPentagon
                        ? LookupTables.PolarPentagonRotationsInReverse
                        : LookupTables.NonPolarPentagonRotationsInReverse;

                    pentagonRotations = table[(int)revDir, (int)indexLeadingDigit];
                    if (pentagonRotations < 0) throw new Exception("invalid number of pentagon rotations");

                    for (int i = 0; i < pentagonRotations; i += 1) index.RotatePentagonCounterClockwise();
                } else {
                    if (pentagonRotations < 0) throw new Exception("invalid number of pentagon rotations");

                    for (int i = 0; i < pentagonRotations; i += 1) index.RotateCounterClockwise();

                    // Adjust for the different coordinate space in the two base cells.
                    for (int i = 0; i < baseCellRotations; i += 1) index.RotateCounterClockwise();
                }
            } else if (originOnPent && indexOnPent) {
                Direction originLeadingDigit = origin.LeadingNonZeroDirection;
                Direction indexLeadingDigit = index.LeadingNonZeroDirection;

                int withinPentagonRotations = LookupTables.PentagonRotationsInReverse[(int)originLeadingDigit, (int)indexLeadingDigit];
                if (withinPentagonRotations < 0) throw new Exception("invalid number of within pentagon rotations");

                for (int i = 0; i < withinPentagonRotations; i += 1) index.RotateCounterClockwise();
            }

            if (indexOnPent) {
                // TODO: There are cases in h3ToLocalIjk which are failed but not
                // accounted for here - instead just fail if the recovered index is
                // invalid.
                if (index.LeadingNonZeroDirection == Direction.K) {
                    throw new Exception("unable to translate pentagon coordinate to index");
                }
            }

            // do we have a base cell?
            if (baseCell == null) throw new Exception("unable to determine base cell");
            index.BaseCellNumber = baseCell.Cell;

            return index;
        }

    }

}
