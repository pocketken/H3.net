using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(H3.CompareBenchmarks.CompareBenchmarks).Assembly).Run(args);
