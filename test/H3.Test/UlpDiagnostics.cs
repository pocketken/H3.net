using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;

// No namespace, so this SetUpFixture applies to the whole assembly and its
// OneTimeTearDown runs exactly once after every test has executed.
//
// It prints the environment manifest (runtime, OS, arch, CPU intrinsics) alongside
// the per-operation maximum ULP observed against the authoritative libh3 v4.5.0
// reference.  The point is diagnosis speed: when the single ULP gate
// (TestHelpers.MaxUlps) trips, this turns "tests are red" into "ToLatLng.lngDeg went
// 0 -> 4 under <framework> on <arch>", which distinguishes a real port regression
// from a platform/runtime libm change (System.Math delegates transcendentals to the
// platform C runtime, so the reference is only bit-exact on the platform it was
// generated on).
[SetUpFixture]
public class UlpDiagnostics {

    [OneTimeTearDown]
    public void ReportUlpManifest() {
        var lines = new List<string> {
            "===== ULP parity manifest =====",
            $"Framework  : {RuntimeInformation.FrameworkDescription}",
            $"OS         : {RuntimeInformation.OSDescription}",
            $"Arch       : OS {RuntimeInformation.OSArchitecture}, process {RuntimeInformation.ProcessArchitecture}",
            $"Intrinsics : {CpuIntrinsics()}",
            $"Gate       : per-cell MaxUlps = {H3.Test.TestHelpers.MaxUlps}, SphereSumMaxUlps = {H3.Test.TestHelpers.SphereSumMaxUlps}",
            "Per-op max ULP vs authoritative libh3 v4.5.0 (0 = bit-for-bit):",
        };

        var byOp = H3.Test.TestHelpers.MaxUlpByOp.OrderByDescending(kv => kv.Value).ToList();
        if (byOp.Count == 0) {
            lines.Add("  (no parity comparisons ran)");
        } else {
            foreach (var kv in byOp)
                lines.Add($"  {kv.Key,-20} {kv.Value} ULP");
        }

        var text = string.Join(Environment.NewLine, lines);
        TestContext.Progress.WriteLine(text);
        Console.WriteLine(text);

        // Also persist unconditionally: teardown output at non-default logger verbosity
        // has a habit of vanishing on exactly the red run you need it for (someone
        // else's machine / CI). A file artifact next to the test binary always survives.
        try {
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "ulp-manifest.txt");
            System.IO.File.WriteAllText(path, text + Environment.NewLine);
        } catch {
            // diagnostics only: never let manifest persistence fail the run
        }
    }

    private static string CpuIntrinsics() {
#if NET8_0_OR_GREATER
        return $"AVX2={System.Runtime.Intrinsics.X86.Avx2.IsSupported} " +
               $"AVX512F={System.Runtime.Intrinsics.X86.Avx512F.IsSupported} " +
               $"FMA={System.Runtime.Intrinsics.X86.Fma.IsSupported}";
#else
        return "n/a";
#endif
    }
}
