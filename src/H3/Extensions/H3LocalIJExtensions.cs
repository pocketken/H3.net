using System;
using H3.Model;
using static H3.Utils;

#nullable enable

namespace H3.Extensions;

/// <summary>
/// Extends the H3Index class with support for generating LocalIJ coordinates.
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
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    [Obsolete("as of 4.0: use CellToLocalIj instead")]
    public static CoordIJ ToLocalIJ(this H3Index origin, H3Index index) {
        return origin.CellToLocalIj(index);
    }

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
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static CoordIJ CellToLocalIj(this H3Index origin, H3Index index) =>
        CoordIJ.FromCoordIJK(LocalCoordIJK.ToLocalIJK(origin, index));

    /// <summary>
    /// Produces an index for ij coordinates anchored by an origin.
    ///
    /// The coordinate space used by this function may have deleted
    /// regions or warping due to pentagonal distortion.
    ///
    /// Failure may occur if the index is too far away from the origin
    /// or if the index is on the other side of a pentagon.
    /// </summary>
    /// <param name="origin">an anchoring index for the IJ coordinate system</param>
    /// <param name="coord">IJ coordinates to index</param>
    /// <returns>H3Index for coordinates</returns>
    [Obsolete("as of 4.0: use LocalIjToCell instead")]
    public static H3Index FromLocalIJ(this H3Index origin, CoordIJ coord) {
        return origin.LocalIjToCell(coord);
    }

    /// <summary>
    /// Produces an index for ij coordinates anchored by an origin.
    ///
    /// The coordinate space used by this function may have deleted
    /// regions or warping due to pentagonal distortion.
    ///
    /// Failure may occur if the index is too far away from the origin
    /// or if the index is on the other side of a pentagon.
    /// </summary>
    /// <param name="origin">an anchoring index for the IJ coordinate system</param>
    /// <param name="coord">IJ coordinates to index</param>
    /// <returns>H3Index for coordinates</returns>
    public static H3Index LocalIjToCell(this H3Index origin, CoordIJ coord) {
        try {
            return LocalCoordIJK.ToH3Index(origin, coord.ToCoordIJK());
        } catch {
            return H3Index.Invalid;
        }
    }

}

/// <summary>
/// Extends the H3Index class with support for generating LocalIJK coordinates.
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
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    [Obsolete("as of 4.0: use CellToLocalIjk instead")]
    public static CoordIJK ToLocalIJK(this H3Index origin, H3Index index) {
        return origin.CellToLocalIjk(index);
    }

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
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static CoordIJK CellToLocalIjk(this H3Index origin, H3Index index) =>
        LocalCoordIJK.ToLocalIJK(origin, index);

    /// <summary>
    /// Produces an index for ijk coordinates anchored by an origin.
    ///
    /// The coordinate space used by this function may have deleted
    /// regions or warping due to pentagonal distortion.
    ///
    /// Failure may occur if the index is too far away from the origin
    /// or if the index is on the other side of a pentagon.
    /// </summary>
    /// <param name="origin">an anchoring index for the IJK coordinate system</param>
    /// <param name="coord">IJK coordinates to index</param>
    /// <returns>H3Index for coordinates</returns>
    [Obsolete("as of 4.0: use LocalIjkToCell instead")]
    public static H3Index FromLocalIJK(this H3Index origin, CoordIJK coord) {
        return origin.LocalIjkToCell(coord);
    }

    /// <summary>
    /// Produces an index for ijk coordinates anchored by an origin.
    ///
    /// The coordinate space used by this function may have deleted
    /// regions or warping due to pentagonal distortion.
    ///
    /// Failure may occur if the index is too far away from the origin
    /// or if the index is on the other side of a pentagon.
    /// </summary>
    /// <param name="origin">an anchoring index for the IJK coordinate system</param>
    /// <param name="coord">IJK coordinates to index</param>
    /// <returns>H3Index for coordinates</returns>
    public static H3Index LocalIjkToCell(this H3Index origin, CoordIJK coord) {
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
    /// <param name="destination">index to generate IJ coordinates for</param>
    /// <returns>local IJ coordinates</returns>
    public static CoordIJK ToLocalIJK(H3Index origin, H3Index destination) {
        H3Index index = new(destination);
        var resolution = origin.Resolution;
        if (resolution != index.Resolution) {
            throw new ArgumentOutOfRangeException(nameof(index), "must be same resolution as origin");
        }

        var originBaseCell = origin.BaseCell;
        var baseCell = index.BaseCell;

        // Direction from origin base cell to index base cell
        var dir = Direction.Center;
        var revDir = Direction.Center;

        if (originBaseCell != baseCell) {
            dir = BaseCell.GetNeighbourDirection(originBaseCell.Cell, baseCell.Cell);
            if (dir == Direction.Invalid) {
                throw new ArgumentException($"index cell {baseCell.Cell} is not a neighbour of origin cell {originBaseCell.Cell}");
            }

            revDir = BaseCell.GetNeighbourDirection(baseCell.Cell, originBaseCell.Cell);
        }

        var originOnPent = originBaseCell.IsPentagon;
        var indexOnPent = baseCell.IsPentagon;

        if (dir != Direction.Center) {
            // Rotate index into the orientation of the origin base cell.
            // cw because we are undoing the rotation into that base cell.
            var baseCellRotations = originBaseCell.NeighbourRotations[(int)dir];
            if (indexOnPent) {
                for (var i = 0; i < baseCellRotations; i += 1) {
                    index.RotatePentagonClockwise();
                    revDir = revDir.RotateClockwise();
                    if (revDir == Direction.K) revDir = revDir.RotateClockwise();
                }
            } else if (baseCellRotations > 0) {
                index.RotateClockwise(baseCellRotations);
                revDir = revDir.RotateClockwise(baseCellRotations);
            }
        }

        var indexFijk = new FaceIJK();
        index.ToFaceWithInitializedFijk(indexFijk);

        if (dir != Direction.Center) {
            if (originBaseCell == baseCell) throw new Exception("assertion failed; origin should not equal index cell");
            if (originOnPent && indexOnPent) throw new Exception("assertion failed; origin and index cannot both be on a pentagon");

            var pentagonRotations = 0;
            var directionRotations = 0;

            if (originOnPent) {
                var originLeadingDigit = origin.LeadingNonZeroDirection;
                if (LookupTables.UnfoldableDirections[(int)originLeadingDigit, (int)dir]) {
                    // TODO: We may be unfolding the pentagon incorrectly in this
                    // case; return an error code until this is guaranteed to be
                    // correct.
                    throw new Exception("origin -> dir results in unfoldable pentagon");
                }

                directionRotations = LookupTables.PentagonRotations[(int)originLeadingDigit, (int)dir];
                pentagonRotations = directionRotations;
            } else if (indexOnPent) {
                var indexLeadingDigit = index.LeadingNonZeroDirection;
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

            for (var i = 0; i < pentagonRotations; i += 1) indexFijk.Coord.RotateClockwise();

            var offset = new CoordIJK(0, 0, 0).ToNeighbour(dir);
            // scale offset based upon resolution
            for (var r = resolution - 1; r >= 0; r -= 1) {
                if (IsResolutionClass3(r + 1)) {
                    offset.DownAperture7CounterClockwise();
                } else {
                    offset.DownAperture7Clockwise();
                }
            }

            for (var i = 0; i < directionRotations; i += 1) offset.RotateClockwise();

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

            var originLeadingDigit = origin.LeadingNonZeroDirection;
            var indexLeadingDigit = index.LeadingNonZeroDirection;

            if (LookupTables.UnfoldableDirections[(int)originLeadingDigit, (int)indexLeadingDigit]) {
                // TODO: We may be unfolding the pentagon incorrectly in this case;
                // return an error code until this is guaranteed to be correct.
                throw new Exception("origin -> index results in unfoldable pentagon");
            }

            var withinPentagonRotations = LookupTables.PentagonRotations[(int)originLeadingDigit, (int)indexLeadingDigit];

            for (var i = 0; i < withinPentagonRotations; i += 1) indexFijk.Coord.RotateClockwise();
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
    public static H3Index ToH3Index(H3Index origin, CoordIJK ijk, CoordIJK? workIjk1 = default, CoordIJK? workIjk2 = default, CoordIJK? workIjk3 = default) {
        var resolution = origin.Resolution;
        var originBaseCell = origin.BaseCell;
        if (originBaseCell == null) throw new Exception("origin is not a valid base cell");
        var originOnPent = originBaseCell.IsPentagon;

        H3Index index = new() {
            Mode = Mode.Cell,
            Resolution = resolution
        };

        if (resolution == 0) {
            if (ijk.I > 1 || ijk.J > 1 || ijk.K > 1) throw new Exception("input coordinates out of range");

            var newBaseCell = originBaseCell.NeighbouringCells[(sbyte)(Direction)ijk];
            if (newBaseCell == LookupTables.INVALID_BASE_CELL) throw new Exception("moved in invalid direction off pentagon");

            index.BaseCellNumber = newBaseCell;
            return index;
        }

        // we need to find the correct base cell offset (if any) for this H3 index;
        // start with the passed in base cell and resolution res ijk coordinates
        // in that base cell's coordinate system
        var ijkCopy = workIjk1 ?? new CoordIJK();
        ijkCopy.I = ijk.I;
        ijkCopy.J = ijk.J;
        ijkCopy.K = ijk.K;

        // build the H3Index from finest res up
        // adjust r for the fact that the res 0 base cell offsets the indexing
        // digits
        var lastIJK = workIjk2 ?? new CoordIJK();
        var lastCenter = workIjk3 ?? new CoordIJK();
        for (var r = resolution - 1; r >= 0; r -= 1) {
            lastIJK.I = ijkCopy.I;
            lastIJK.J = ijkCopy.J;
            lastIJK.K = ijkCopy.K;

            if (IsResolutionClass3(r + 1)) {
                // rotate ccw
                ijkCopy.UpAperture7CounterClockwise();
                lastCenter.I = ijkCopy.I;
                lastCenter.J = ijkCopy.J;
                lastCenter.K = ijkCopy.K;
                lastCenter.DownAperture7CounterClockwise();
            } else {
                // rotate cw
                ijkCopy.UpAperture7Clockwise();
                lastCenter.I = ijkCopy.I;
                lastCenter.J = ijkCopy.J;
                lastCenter.K = ijkCopy.K;
                lastCenter.DownAperture7Clockwise();
            }

            lastIJK.I -= lastCenter.I;
            lastIJK.J -= lastCenter.J;
            lastIJK.K -= lastCenter.K;
            index.SetDirectionForResolution(r + 1, lastIJK);
        }

        // ijkCopy should now hold the IJK of the base cell in the
        // coordinate system of the current base cell
        if (ijkCopy.I > 1 || ijkCopy.J > 1 || ijkCopy.K > 1) throw new Exception("input is out of range");

        // lookup correct base cell
        Direction dir = ijkCopy;
        var baseCell = originBaseCell.Neighbour(dir);

        // If baseCell is invalid, it must be because the origin base cell is a
        // pentagon, and because pentagon base cells do not border each other,
        // baseCell must not be a pentagon.
        var indexOnPent = baseCell != null && baseCell.IsPentagon;

        if (dir != Direction.Center) {
            // If the index is in a warped direction, we need to unwarp the base
            // cell direction. There may be further need to rotate the index digits.
            var pentagonRotations = 0;

            if (originOnPent) {
                var originLeadingDigit = origin.LeadingNonZeroDirection;
                pentagonRotations = LookupTables.PentagonRotationsInReverse[(int)originLeadingDigit, (int)dir];

                dir = dir.RotateCounterClockwise(pentagonRotations);

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
            var baseCellRotations = originBaseCell.NeighbourRotations[(sbyte)dir];
            if (baseCellRotations < 0) throw new Exception("invalid number of base cell rotations");

            // Adjust for pentagon warping within the base cell. The base cell
            // should be in the right location, so now we need to rotate the index
            // back. We might not need to check for errors since we would just be
            // double mapping.
            if (indexOnPent) {
                var revDir = BaseCell.GetNeighbourDirection(baseCell.Cell, originBaseCell.Cell);
                if (revDir == Direction.Invalid) throw new Exception("invalid rotation direction");

                // Adjust for the different coordinate space in the two base cells.
                // This is done first because we need to do the pentagon rotations
                // based on the leading digit in the pentagon's coordinate system.
                if (baseCellRotations > 0) index.RotateCounterClockwise(baseCellRotations);

                var indexLeadingDigit = index.LeadingNonZeroDirection;
                var table = baseCell.IsPolarPentagon
                    ? LookupTables.PolarPentagonRotationsInReverse
                    : LookupTables.NonPolarPentagonRotationsInReverse;

                pentagonRotations = table[(int)revDir, (int)indexLeadingDigit];
                if (pentagonRotations < 0) throw new Exception("invalid number of pentagon rotations");

                for (var i = 0; i < pentagonRotations; i += 1) index.RotatePentagonCounterClockwise();
            } else {
                switch (pentagonRotations) {
                    case < 0:
                        throw new Exception("invalid number of pentagon rotations");
                    case > 0:
                        index.RotateCounterClockwise(pentagonRotations);
                        break;
                }

                // Adjust for the different coordinate space in the two base cells.
                if (baseCellRotations > 0) index.RotateCounterClockwise(baseCellRotations);
            }
        } else if (originOnPent && indexOnPent) {
            var originLeadingDigit = origin.LeadingNonZeroDirection;
            var indexLeadingDigit = index.LeadingNonZeroDirection;

            var withinPentagonRotations = LookupTables.PentagonRotationsInReverse[(int)originLeadingDigit, (int)indexLeadingDigit];
            switch (withinPentagonRotations) {
                case < 0:
                    throw new Exception("invalid number of within pentagon rotations");
                case > 0:
                    index.RotateCounterClockwise(withinPentagonRotations);
                    break;
            }
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