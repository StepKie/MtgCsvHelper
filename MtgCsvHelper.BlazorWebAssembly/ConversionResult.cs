using MtgCsvHelper.Models;

namespace MtgCsvHelper.BlazorWebAssembly;

/// <summary>
/// Bundles everything produced by a single conversion run so the page tracks one nullable
/// reference (present = run done, null = pristine) instead of a dozen parallel fields.
/// Owns the output stream and disposes it on reset / page teardown.
/// </summary>
public sealed class ConversionResult : IDisposable
{
	public required string InputFormat { get; init; }
	public required string OutputFormat { get; init; }
	public required DateTime ImportedAt { get; init; }
	public required IReadOnlyList<PhysicalMtgCard> Records { get; init; }
	public required IReadOnlyList<ImportIssue> Issues { get; init; }
	public required CollectionSummary Summary { get; init; }
	public MemoryStream? OutputStream { get; init; }
	public string? OutputFileName { get; init; }

	public void Dispose() => OutputStream?.Dispose();
}
