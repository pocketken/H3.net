// SPDX-License-Identifier: Apache-2.0

using System;
using System.Runtime.InteropServices;

namespace H3.NET.Native.Benchmarks;

/// <summary>
/// Direct, dependency-free P/Invoke into libh3 used as the benchmark baseline for
/// the two operations whose raw C signatures marshal trivially (latLngToCell and
/// gridDisk). It deliberately re-declares its own [LibraryImport] surface rather
/// than reaching into the binding's internal NativeMethods: the baseline must
/// measure the floor cost of crossing into native code with zero managed wrapping,
/// so it can only legitimately serve as that floor if it shares none of the
/// binding's code. polygonToCells is intentionally NOT baselined here: its C entry
/// takes a GeoPolygon* with nested GeoLoop arrays, so a "raw" version would have to
/// duplicate the binding's marshalling and would no longer isolate thin overhead.
/// </summary>
internal static unsafe partial class RawH3Native
{
    // libh3's LatLng is RADIANS at the ABI, unlike the binding's public LatLng which
    // accepts degrees and converts at the boundary; the helpers below convert the
    // same degree inputs so this baseline indexes the identical point.
    [StructLayout(LayoutKind.Sequential)]
    private struct RawLatLng
    {
        public double Lat;
        public double Lng;
    }

    [LibraryImport("h3", EntryPoint = "latLngToCell")]
    private static partial uint LatLngToCell(in RawLatLng g, int res, out ulong outCell);

    [LibraryImport("h3", EntryPoint = "maxGridDiskSize")]
    private static partial uint MaxGridDiskSize(int k, out long size);

    [LibraryImport("h3", EntryPoint = "gridDisk")]
    private static partial uint GridDisk(ulong origin, int k, ulong* outCells);

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    /// <summary>
    /// Indexes the given degree coordinate to a cell via a bare latLngToCell call,
    /// converting to radians first so it matches the binding's public degree input.
    /// </summary>
    internal static ulong LatLngToCell(double latDegrees, double lngDegrees, int resolution)
    {
        var point = new RawLatLng
        {
            Lat = DegreesToRadians(latDegrees),
            Lng = DegreesToRadians(lngDegrees),
        };

        _ = LatLngToCell(in point, resolution, out var cell);
        return cell;
    }

    /// <summary>
    /// Expands a cell by gridDisk and returns the number of real (non-H3_NULL)
    /// cells, mirroring what the binding's GridDisk(...).Length reports. The out
    /// buffer is sized via maxGridDiskSize and unused slots are the H3_NULL (0)
    /// sentinel, so counting non-zero entries yields the true disk size.
    /// </summary>
    internal static int GridDiskCount(ulong origin, int k)
    {
        _ = MaxGridDiskSize(k, out var size);

        // Worst-case maxGridDiskSize is small for the benchmark k, so a stack buffer
        // keeps the baseline free of managed allocation noise.
        Span<ulong> cells = stackalloc ulong[(int)size];

        fixed (ulong* cellsPtr = cells)
        {
            _ = GridDisk(origin, k, cellsPtr);
        }

        var count = 0;
        foreach (var cell in cells)
        {
            if (cell != 0)
            {
                count++;
            }
        }

        return count;
    }
}
