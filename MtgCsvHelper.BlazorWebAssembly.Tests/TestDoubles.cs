using MtgCsvHelper.Services;

namespace MtgCsvHelper.BlazorWebAssembly.Tests;

/// <summary> Catalog loader with settable state; defaults to a ready, empty catalog. </summary>
sealed class FakeCatalogLoader : ICatalogLoader
{
	public IReferenceCardCatalog? Catalog { get; set; } = new ReferenceCardCatalog([]);
	public CatalogLoadProgress Progress { get; set; } = new(CatalogLoadPhase.Ready, 100, 0, null);
	public Exception? Error { get; set; }
	public event Action? StateChanged;
	public int LoadCalls { get; private set; }

	public Task LoadAsync(CancellationToken ct = default)
	{
		LoadCalls++;
		return Task.CompletedTask;
	}

	public void RaiseStateChanged() => StateChanged?.Invoke();
}

sealed class FakeCardmarketResolver : ICardmarketResolver
{
	public Task<IReadOnlyDictionary<int, ReferenceCard>> ResolveAsync(IEnumerable<int> cardmarketIds, CancellationToken ct = default) =>
		Task.FromResult<IReadOnlyDictionary<int, ReferenceCard>>(new Dictionary<int, ReferenceCard>());
}
