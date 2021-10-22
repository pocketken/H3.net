using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Linq;
using H3.Test;
using H3.Model;
using H3Lib.Extensions;

namespace H3.Benchmarks {

    [SimpleJob(RuntimeMoniker.Net50)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [SimpleJob(RuntimeMoniker.Net48)]
    [MemoryDiagnoser]
    public class H3IndexBenchmarks {

        private static readonly H3Index TestIndex = TestHelpers.TestIndexValue;
        private static readonly H3Index InvalidIndex = new(TestIndex);
        private static readonly H3Index PentagonIndex = LookupTables.PentagonIndexesPerResolution[14].First();
        private static readonly H3Lib.H3Index TestH3LibIndex = new(TestIndex);
        private static readonly H3Lib.H3Index PentagonH3LibIndex = new(PentagonIndex);
        private static readonly H3Lib.H3Index NullH3LibIndex = new(H3Index.Invalid);
        private H3Lib.H3Index _invalidH3LibIndex;

        [GlobalSetup]
        public void Setup() {
            InvalidIndex.SetDirectionForResolution(10, Direction.Invalid);
            _invalidH3LibIndex = new H3Lib.H3Index(InvalidIndex);
        }

        [Benchmark(Description = "pocketken.H3.H3Index.IsValid(hex)")]
        public bool PocketkenIsValid() => TestIndex.IsValid;

        [Benchmark(Description = "pocketken.H3.H3Index.IsValid(pent)")]
        public bool PocketkenIsValidPent() => PentagonIndex.IsValid;

        [Benchmark(Description = "pocketken.H3.H3Index.IsValid(null)")]
        public bool PocketkenIsValidNull() => H3Index.Invalid.IsValid;

        [Benchmark(Description = "pocketken.H3.H3Index.IsValid(invalid)")]
        public bool PocketkenIsValidInvalid() => InvalidIndex.IsValid;

        [Benchmark(Description = "H3Lib.Extensions.H3IndexExtensions.IsValid(hex)")]
        public bool H3LibIsValid() => TestH3LibIndex.IsValid();

        [Benchmark(Description = "H3Lib.Extensions.H3IndexExtensions.IsValid(pent)")]
        public bool H3LibIsValidPent() => PentagonH3LibIndex.IsValid();

        [Benchmark(Description = "H3Lib.Extensions.H3IndexExtensions.IsValid(null)")]
        public bool H3LibIsValidNull() => NullH3LibIndex.IsValid();

        [Benchmark(Description = "H3Lib.Extensions.H3IndexExtensions.IsValid(invalid)")]
        public bool H3LibIsValidInvalid() => _invalidH3LibIndex.IsValid();

        [Benchmark(Description = "pocketken.H3.H3Index.RotateClockwise(hex)")]
        public void PocketkenRotateHexClockwise() => TestIndex.RotateClockwise();

        [Benchmark(Description = "pocketken.H3.H3Index.RotateCounterClockwise(hex)")]
        public void PocketkenRotateHexCounterClockwise() => TestIndex.RotateCounterClockwise();

        [Benchmark(Description = "pocketken.H3.H3Index.RotateClockwise(pent)")]
        public void PocketkenRotatePentagonClockwise() => PentagonIndex.RotatePentagonClockwise();

        [Benchmark(Description = "pocketken.H3.H3Index.RotateCounterClockwise(pent)")]
        public void PocketkenRotatePentagonCounterClockwise() => PentagonIndex.RotatePentagonCounterClockwise();

        [Benchmark(Description = "pocketken.H3.H3Index.RotateClockwise(hex, 4)")]
        public void PocketkenRotateHexClockwise4() => TestIndex.RotateClockwise(4);

        [Benchmark(Description = "pocketken.H3.H3Index.RotateCounterClockwise(hex, 4)")]
        public void PocketkenRotateHexCounterClockwise4() => TestIndex.RotateCounterClockwise(4);

        [Benchmark(Description = "pocketken.H3.H3Index.LeadingNonZeroDirection")]
        public Direction LeadingNonZeroDirection() => TestIndex.LeadingNonZeroDirection;

    }
}
