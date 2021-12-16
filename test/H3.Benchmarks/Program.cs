using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace H3.Benchmarks
{
    [MemoryDiagnoser]
    public class Program {
        public static void Main(string[] args) => BenchmarkSwitcher.FromAssemblies(new[] { typeof(Program).Assembly }).Run(args);
    }

}
