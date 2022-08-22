using BenchmarkDotNet.Attributes;
using System;
using System.Numerics;

namespace H3.Benchmarks; 

[Config(typeof(LatestVersionBenchmarkConfig))]
[MemoryDiagnoser]
public class MathBenchmarks {

    private const double Value = 0.2387273897282832;
    private const int TestInt = 1;

    [Benchmark(Description="Math.Round", Baseline = true)]
    public int MathRound() => (int)Math.Round(Value);

    [Benchmark(Description = "Math.Round with midpoint away from zero")]
    public int MathRoundMidpoint() => (int)Math.Round(Value, MidpointRounding.AwayFromZero);

    [Benchmark(Description = "C-Like Round With Sign Check")]
    public double CRound() => double.IsNegative(Value) ? Math.Ceiling(Value - 0.5) : Math.Floor(Value + 0.5);

    [Benchmark(Description = "int.LeadingZeros, JDK style")]
    public int IntLeadingZerosJava() => LeadingZerosJava(TestInt);

    [Benchmark(Description = "int.LeadingZeros, Smearing style")]
    public int IntLeadingZerosSO10439242() => LeadingZeros(TestInt);

    [Benchmark(Description = "int.LeadingZeros, BitOperations")]
    public int IntLeadingZerosBitOperations() => BitOperations.LeadingZeroCount(TestInt);

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LeadingZerosJava(int value) {
        if (value <= 0) return value == 0 ? 32 : 0;
        var n = 31;
        if (value >= 1 << 16) {
            n -= 16;
            value >>= 16;
        }
        if (value >= 1 << 8) {
            n -= 8;
            value >>= 8;
        }
        if (value >= 1 << 4) {
            n -= 4;
            value >>= 4;
        }
        if (value >= 1 << 2) {
            n -= 2;
            value >>= 2;
        }
        return n - (value >> 1);
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LeadingZeros(int value) {
        if (value <= 0) return value == 0 ? 32 : 0;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        value -= value >> 1 & 0x55555555;
        value = (value >> 2 & 0x33333333) + (value & 0x33333333);
        value = (value >> 4) + value & 0x0f0f0f0f;
        value += value >> 8;
        value += value >> 16;
        return 32 - (value & 0x0000003f);
    }
}