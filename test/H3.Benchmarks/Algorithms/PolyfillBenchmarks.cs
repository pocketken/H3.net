using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Linq;
using H3.Algorithms;
using H3Lib;
using NetTopologySuite.Geometries;
using static H3.Constants;
using static H3.Utils;
using GeoCoord = H3.Model.GeoCoord;

namespace H3.Benchmarks.Algorithms {

    [SimpleJob(RuntimeMoniker.Net50)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [SimpleJob(RuntimeMoniker.Net48)]
    [MemoryDiagnoser]
    public class PolyfillBenchmarks {

        private static readonly GeoCoord[] EntireWorld = {
            (M_PI_2, -M_PI),
            (M_PI_2, M_PI),
            (-M_PI_2, M_PI),
            (-M_PI_2, -M_PI),
            (M_PI_2, -M_PI),
        };

        // coordinates for the upstream lib's "SF" test poly
        private static readonly GeoCoord[] UberSfTestPoly = {
            (0.659966917655, -2.1364398519396),
            (0.6595011102219, -2.1359434279405),
            (0.6583348114025, -2.1354884206045),
            (0.6581220034068, -2.1382437718946),
            (0.6594479998527, -2.1384597563896),
            (0.6599990002976, -2.1376771158464),
            (0.659966917655, -2.1364398519396)
        };

        private static readonly Polygon EntireWorldGeometry = DefaultGeometryFactory.CreatePolygon(EntireWorld.Select(g => g.ToCoordinate()).Reverse().ToArray());

        private static readonly Polygon SfGeometry = DefaultGeometryFactory.CreatePolygon(UberSfTestPoly.Select(g => g.ToCoordinate()).Reverse().ToArray());

        //private static readonly GeoPolygon EntireWorldGeoPolygon = new() {
        //    GeoFence = new GeoFence {
        //        NumVerts = EntireWorld.Length - 1,
        //        Verts = EntireWorld.SkipLast(1).Select(g => new H3Lib.GeoCoord(Convert.ToDecimal(g.Latitude), Convert.ToDecimal(g.Longitude))).ToArray()
        //    }
        //};

        //private static readonly GeoPolygon SfGeoPolygon = new() {
        //    GeoFence = new GeoFence {
        //        NumVerts = UberSfTestPoly.Length - 1,
        //        Verts = UberSfTestPoly.SkipLast(1).Select(g => new H3Lib.GeoCoord(Convert.ToDecimal(g.Latitude), Convert.ToDecimal(g.Longitude))).ToArray()
        //    }
        //};

        [Benchmark(Description = "pocketken.H3.Fill(worldPolygon, 4)")]
        public int PolyfillWorld4() => EntireWorldGeometry.Fill(4).Count();

        [Benchmark(Description = "pocketken.H3.Fill(worldPolygon, 5)")]
        public int PolyfillWorld5() => EntireWorldGeometry.Fill(5).Count();

        //[Benchmark(Description = "H3Lib.Polyfill(worldPolygon, 4)")]
        //public int H3LibPolyfillWorld() => EntireWorldGeoPolygon.Polyfill(4).Count;

        [Benchmark(Description = "pocketken.H3.Fill(sfPolygon, 10)")]
        public int PolyfillSf10() => SfGeometry.Fill(10).Count();

        //[Benchmark(Description = "H3Lib.Polyfill(sfPolygon, 10)")]
        //public int H3LibPolyfillSf() => SfGeoPolygon.Polyfill(10).Count;

        [Benchmark(Description = "pocketken.H3.Fill(sfPolygon, 11)")]
        public int PolyfillSf11() => SfGeometry.Fill(11).Count();

        //[Benchmark(Description = "H3Lib.Polyfill(sfPolygon, 11)")]
        //public int H3LibPolyfillSf11() => SfGeoPolygon.Polyfill(11).Count;

        [Benchmark(Description = "pocketken.H3.Fill(sfPolygon, 12)")]
        public int PolyfillSf12() => SfGeometry.Fill(12).Count();

        //[Benchmark(Description = "H3Lib.Polyfill(sfPolygon, 12)")]
        //public int H3LibPolyfillSf12() => SfGeoPolygon.Polyfill(12).Count;

        //[Benchmark(Description = "pocketken.H3.Fill(sfPolygon, 13)")]
        //public int PolyfillSf13() => SfGeometry.Fill(13).Count();

        //[Benchmark(Description = "pocketken.H3.Fill(sfPolygon, 14)")]
        //public int PolyfillSf14() => SfGeometry.Fill(14).Count();

        //[Benchmark(Description = "pocketken.H3.Fill(sfPolygon, 15)")]
        //public int PolyfillSf15() => SfGeometry.Fill(15).Count();

    }

}