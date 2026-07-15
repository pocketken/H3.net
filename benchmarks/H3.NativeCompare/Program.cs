// SPDX-License-Identifier: Apache-2.0
using BenchmarkDotNet.Running;

namespace H3.NET.Native.Benchmarks;

/// <summary>
/// Entry point. Use the BenchmarkDotNet switcher so individual benchmark
/// classes can be selected from the command line (e.g. <c>--filter *</c>).
/// Benchmarks are warn-only and never gate CI; this project only needs to build
/// and be runnable on demand in Release.
/// </summary>
internal static class Program
{
    private static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
