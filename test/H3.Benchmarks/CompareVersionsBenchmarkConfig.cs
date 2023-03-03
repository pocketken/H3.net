using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace H3.Benchmarks;

public abstract class H3BenchmarkConfig : ManualConfig {

    protected static string GetDevPackagePath() {
        var codeBase = Assembly.GetExecutingAssembly().Location;
        var uri = new UriBuilder(codeBase);
        var path = Uri.UnescapeDataString(uri.Path);
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path) ?? throw new InvalidOperationException(), @"../../../../../packages"));
    }

}

public class CompareVersionsBenchmarkConfig : H3BenchmarkConfig {

    public CompareVersionsBenchmarkConfig() {
        var packagePath = GetDevPackagePath();
        Console.WriteLine($" * using local package path: {packagePath}");
        foreach (var job in GetJobs("4.0.0", "4.1.0-preview")) {
            AddJob(job);
        }
    }

    private static IEnumerable<Job> GetJobs(params string[] versions) {
        var packagePath = new Uri(GetDevPackagePath());
        for (var v = 0; v < versions.Length; v += 1) {
            var version = versions[v];
            var baseJob = Job.MediumRun
                .WithNuGet("pocketken.H3", version, version.Contains("-preview") ? packagePath : null);
            yield return baseJob.WithRuntime(CoreRuntime.Core70).WithBaseline(v == 0).WithId(version);
        }
    }

}

public class LatestVersionBenchmarkConfig : H3BenchmarkConfig {

    public LatestVersionBenchmarkConfig() {
        var packagePath = new Uri(GetDevPackagePath());
        Console.WriteLine($" * using local package path: {packagePath}");
        AddJob(Job.MediumRun
            .WithNuGet("pocketken.H3", "4.1.0-preview", packagePath)
            .WithRuntime(CoreRuntime.Core70)
            .WithId("4.1.0-preview"));
    }

}
