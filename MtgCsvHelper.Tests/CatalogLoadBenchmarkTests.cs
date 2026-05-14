using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

namespace MtgCsvHelper.Tests;

/// <summary>
/// Measures the catalog load phases on the local CLR. Reported numbers are the *fast*
/// baseline — WASM interpreter is roughly 10-20× slower across the board, so multiply
/// these by ~15 for a rough estimate of the Blazor experience without AOT.
///
/// Run from CLI with verbose output to see timings:
///   dotnet test --filter "FullyQualifiedName~CatalogLoadBenchmark" -v normal
/// </summary>
public class CatalogLoadBenchmarkTests(ITestOutputHelper output) : BaseTest(output)
{
	static string BundlePath => Path.Combine(AppContext.BaseDirectory, "data", "cards.min.json.gz");

	[Fact]
	public async Task Phase_Breakdown_ProductionPath()
	{
		if (!File.Exists(BundlePath))
		{
			output.WriteLine($"Bundle missing at {BundlePath}; benchmark skipped.");
			return;
		}

		var bytes = await File.ReadAllBytesAsync(BundlePath);
		output.WriteLine($"Bundle on disk: {bytes.Length:N0} bytes ({bytes.Length / 1024.0 / 1024.0:0.##} MB compressed)");

		// Phase 1: decompress only
		var sw = Stopwatch.StartNew();
		await using (var src = new MemoryStream(bytes))
		using (var gzip = new GZipStream(src, CompressionMode.Decompress))
		await using (var sink = new MemoryStream())
		{
			await gzip.CopyToAsync(sink);
			output.WriteLine($"  Decompress: {sw.ElapsedMilliseconds} ms → {sink.Length:N0} bytes raw JSON");
		}

		// Phase 2: deserialize (reflection-based path used in production)
		sw.Restart();
		List<ReferenceCard>? cards;
		await using (var src = new MemoryStream(bytes))
		using (var gzip = new GZipStream(src, CompressionMode.Decompress))
		{
			cards = await JsonSerializer.DeserializeAsync<List<ReferenceCard>>(gzip, ReferenceCardCatalog.BundleSerializerOptions);
		}
		var parseMs = sw.ElapsedMilliseconds;
		output.WriteLine($"  Parse: {parseMs} ms → {cards!.Count:N0} cards");

		// Phase 3: index build
		sw.Restart();
		var catalog = new ReferenceCardCatalog(cards);
		output.WriteLine($"  Index build: {sw.ElapsedMilliseconds} ms ({catalog.Count:N0} printings, {catalog.GetSets().Count:N0} sets)");

		// Total production path (decompress + parse + index)
		sw.Restart();
		await using (var src = new MemoryStream(bytes))
		{
			_ = await ReferenceCardCatalog.LoadGzipAsync(src);
		}
		output.WriteLine($"Total LoadGzipAsync: {sw.ElapsedMilliseconds} ms (CLR; WASM interpreter ≈ ×15, AOT ≈ same as CLR ×~1-3)");
	}

}

static class Skip
{
	public static void IfBundleMissing(ITestOutputHelper output)
	{
		var p = Path.Combine(AppContext.BaseDirectory, "data", "cards.min.json.gz");
		if (!File.Exists(p))
		{
			output.WriteLine($"Bundle missing at {p}; skipping.");
		}
	}
}
