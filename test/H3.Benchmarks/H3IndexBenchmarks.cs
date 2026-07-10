using BenchmarkDotNet.Attributes;
using System.Linq;
using H3.Extensions;
using H3.Model;

namespace H3.Benchmarks;

[Config(typeof(CompareVersionsBenchmarkConfig))]
[MemoryDiagnoser]
public class H3IndexBenchmarks {

    private static readonly H3Index TestIndex = Common.TestIndexValue;
    private static readonly H3Index InvalidIndex = new(TestIndex);
    private static readonly H3Index PentagonIndex = LookupTables.PentagonIndexesPerResolution[14].First();
    //private static readonly H3Lib.H3Index TestH3LibIndex = new(TestIndex);
    //private static readonly H3Lib.H3Index PentagonH3LibIndex = new(PentagonIndex);
    //private static readonly H3Lib.H3Index NullH3LibIndex = new(H3Index.Invalid);
    //private H3Lib.H3Index _invalidH3LibIndex;

    private static readonly H3Index[] LeadingZeroTests = {
        H3Index.Create(0, 5, Direction.Center).GetChildCenterForResolution(1).GetDirectNeighbour(Direction.JK).Item1,
        H3Index.Create(0, 5, Direction.Center).GetChildCenterForResolution(5).GetDirectNeighbour(Direction.JK).Item1,
        H3Index.Create(0, 5, Direction.Center).GetChildCenterForResolution(10).GetDirectNeighbour(Direction.JK).Item1,
        H3Index.Create(0, 5, Direction.Center).GetChildCenterForResolution(14).GetDirectNeighbour(Direction.JK).Item1,
    };

    [GlobalSetup]
    public void Setup() {
        InvalidIndex.SetDirectionForResolution(10, Direction.Invalid);
        //_invalidH3LibIndex = new H3Lib.H3Index(InvalidIndex);
    }

    // [Benchmark(Description = "pocketken.H3.H3Index.IsValid(hex)")]
    // public bool PocketkenIsValid() => TestIndex.IsValid;
    //
    // [Benchmark(Description = "pocketken.H3.H3Index.IsValid(pent)")]
    // public bool PocketkenIsValidPent() => PentagonIndex.IsValid;
    //
    // [Benchmark(Description = "pocketken.H3.H3Index.IsValid(null)")]
    // public bool PocketkenIsValidNull() => H3Index.Invalid.IsValid;
    //
    // [Benchmark(Description = "pocketken.H3.H3Index.IsValid(invalid)")]
    // public bool PocketkenIsValidInvalid() => InvalidIndex.IsValid;
    //
    // //[Benchmark(Description = "H3Lib.Extensions.H3IndexExtensions.IsValid(hex)")]
    // //public bool H3LibIsValid() => TestH3LibIndex.IsValid();
    //
    // //[Benchmark(Description = "H3Lib.Extensions.H3IndexExtensions.IsValid(pent)")]
    // //public bool H3LibIsValidPent() => PentagonH3LibIndex.IsValid();
    //
    // //[Benchmark(Description = "H3Lib.Extensions.H3IndexExtensions.IsValid(null)")]
    // //public bool H3LibIsValidNull() => NullH3LibIndex.IsValid();
    //
    // //[Benchmark(Description = "H3Lib.Extensions.H3IndexExtensions.IsValid(invalid)")]
    // //public bool H3LibIsValidInvalid() => _invalidH3LibIndex.IsValid();
    //
    // [Benchmark(Description = "pocketken.H3.H3Index.RotateClockwise(hex)")]
    // public void PocketkenRotateHexClockwise() => TestIndex.RotateClockwise();
    //
    // [Benchmark(Description = "pocketken.H3.H3Index.RotateCounterClockwise(hex)")]
    // public void PocketkenRotateHexCounterClockwise() => TestIndex.RotateCounterClockwise();

    [Benchmark(Description = "pocketken.H3.H3Index.RotateClockwise(pent)")]
    public void PocketkenRotatePentagonClockwise() => PentagonIndex.RotatePentagonClockwise();

    [Benchmark(Description = "pocketken.H3.H3Index.RotateCounterClockwise(pent)")]
    public void PocketkenRotatePentagonCounterClockwise() => PentagonIndex.RotatePentagonCounterClockwise();

    // [Benchmark(Description = "pocketken.H3.H3Index.RotateClockwise(hex, 4)")]
    // public void PocketkenRotateHexClockwise4() => TestIndex.RotateClockwise(4);
    //
    // [Benchmark(Description = "pocketken.H3.H3Index.RotateCounterClockwise(hex, 4)")]
    // public void PocketkenRotateHexCounterClockwise4() => TestIndex.RotateCounterClockwise(4);
    //
    [Benchmark(Description = "pocketken.H3.H3Index.LeadingNonZeroDirection(TestIndex)")]
    public Direction LeadingNonZeroDirectionTestIndex() => TestIndex.LeadingNonZeroDirection;

    // [Benchmark(Description = "pocketken.H3.H3Index.LeadingNonZeroDirection(PentagonIndex)")]
    // public Direction LeadingNonZeroDirectionPentagonIndex() => PentagonIndex.LeadingNonZeroDirection;

    // [Benchmark(Description = "pocketken.H3.H3Index.LeadingNonZeroDirection(LeadingZeroTests[0])")]
    // public Direction LeadingNonZeroDirectionRes1() => LeadingZeroTests[0].LeadingNonZeroDirection;
    // [Benchmark(Description = "pocketken.H3.H3Index.LeadingNonZeroDirection(LeadingZeroTests[1])")]
    // public Direction LeadingNonZeroDirectionRes5() => LeadingZeroTests[1].LeadingNonZeroDirection;
    // [Benchmark(Description = "pocketken.H3.H3Index.LeadingNonZeroDirection(LeadingZeroTests[2])")]
    // public Direction LeadingNonZeroDirectionRes10() => LeadingZeroTests[2].LeadingNonZeroDirection;
    // [Benchmark(Description = "pocketken.H3.H3Index.LeadingNonZeroDirection(LeadingZeroTests[3])")]
    // public Direction LeadingNonZeroDirectionRes14() => LeadingZeroTests[3].LeadingNonZeroDirection;

}
