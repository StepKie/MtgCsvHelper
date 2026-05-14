using System.Diagnostics.CodeAnalysis;
using Serilog;

namespace MtgCsvHelper.BlazorWebAssembly;

public enum CatalogLoadPhase { Idle, Downloading, Parsing, Ready, Failed }

public sealed record CatalogLoadProgress(CatalogLoadPhase Phase, int Percent, long BytesRead, long? TotalBytes)
{
	public static readonly CatalogLoadProgress Idle = new(CatalogLoadPhase.Idle, 0, 0, null);
}

public interface ICatalogLoader
{
	IReferenceCardCatalog? Catalog { get; }
	CatalogLoadProgress Progress { get; }
	Exception? Error { get; }
	[SuppressMessage("Design", "CA1003:Use generic event handler instances", Justification = "Action is intentional — subscribers only need a re-render ping, not args.")]
	event Action? StateChanged;
	Task LoadAsync(CancellationToken ct = default);
}

/// <summary>
/// Loads the Scryfall reference-card bundle in the background after the app shell has
/// rendered. Replaces the eager fetch in <c>Program.cs</c> that blocked first paint for
/// ~10-30 seconds while ~80k cards were parsed on the WASM interpreter.
/// </summary>
/// <remarks>
/// Subscribers get re-rendered via <see cref="StateChanged"/> on each progress tick.
/// Idempotent: calling <see cref="LoadAsync"/> twice on the same instance returns
/// immediately after the first call has started.
/// </remarks>
public sealed class CatalogLoader(HttpClient http) : ICatalogLoader
{
	const long EstimatedBundleBytes = 12_000_000;  // dev server omits Content-Length; production has it

	readonly HttpClient _http = http;
	// 0 = idle or failed (retryable); 1 = in-flight or succeeded. Reset to 0 in the catch
	// so RetryCatalogLoad can call LoadAsync again after a failure.
	int _started;

	public IReferenceCardCatalog? Catalog { get; private set; }
	public CatalogLoadProgress Progress { get; private set; } = CatalogLoadProgress.Idle;
	public Exception? Error { get; private set; }
	[SuppressMessage("Design", "CA1003:Use generic event handler instances", Justification = "Action is intentional — subscribers only need a re-render ping, not args.")]
	public event Action? StateChanged;

	public async Task LoadAsync(CancellationToken ct = default)
	{
		if (Interlocked.CompareExchange(ref _started, 1, 0) != 0) return;
		Error = null;

		try
		{
			using var response = await _http.GetAsync("data/cards.min.json.gz", HttpCompletionOption.ResponseHeadersRead, ct);
			response.EnsureSuccessStatusCode();

			var totalBytes = response.Content.Headers.ContentLength is > 0
				? response.Content.Headers.ContentLength.Value
				: EstimatedBundleBytes;
			await using var responseStream = await response.Content.ReadAsStreamAsync(ct);
			var buffer = new MemoryStream((int)Math.Min(totalBytes, int.MaxValue));
			var chunk = new byte[64 * 1024];
			long read = 0;
			int lastReportedPct = -1;
			int n;
			while ((n = await responseStream.ReadAsync(chunk, ct)) > 0)
			{
				await buffer.WriteAsync(chunk.AsMemory(0, n), ct);
				read += n;
				// Cap at 99 during download — 100 is reserved for "parse done, ready to use".
				var pct = (int)Math.Min(99, read * 100 / totalBytes);
				if (pct != lastReportedPct)
				{
					lastReportedPct = pct;
					SetProgress(new CatalogLoadProgress(CatalogLoadPhase.Downloading, pct, read, totalBytes));
				}
			}

			SetProgress(new CatalogLoadProgress(CatalogLoadPhase.Parsing, 99, read, totalBytes));

			buffer.Position = 0;
			var catalog = await ReferenceCardCatalog.LoadGzipAsync(buffer, ct);

			Catalog = catalog;
			SetProgress(new CatalogLoadProgress(CatalogLoadPhase.Ready, 100, read, totalBytes));
			Log.Information("Loaded reference catalog: {Count:N0} printings.", catalog.Count);
		}
		catch (OperationCanceledException)
		{
			// Component teardown — silent. Reset so a fresh LoadAsync can run after the
			// cancelled one (would otherwise leave the loader permanently un-retryable).
			Interlocked.Exchange(ref _started, 0);
			throw;
		}
		catch (Exception ex)
		{
			Error = ex;
			SetProgress(new CatalogLoadProgress(CatalogLoadPhase.Failed, 0, 0, null));
			Log.Error(ex, "Background catalog load failed");
			// Reset so callers can retry — the idempotency guard at the top is for in-flight
			// double-LoadAsync, not for "load once, never retry".
			Interlocked.Exchange(ref _started, 0);
		}
	}

	void SetProgress(CatalogLoadProgress next)
	{
		Progress = next;
		StateChanged?.Invoke();
	}
}
