using MtgCsvHelper.Services;
using Serilog;

namespace MtgCsvHelper.Tests;

/// <summary>
/// Loads the bundled reference catalog once for the entire test run, plus an
/// <see cref="ICardmarketResolver"/> wired over a real <see cref="CachedMtgApi"/> for the
/// cardmarket-fallback path. Tests that don't need either can stick with <see cref="BaseTest"/>;
/// tests that touch handlers or the catalog extend <see cref="ApiBaseTest"/>.
/// </summary>
public class CatalogFixture : IAsyncLifetime
{
	public IReferenceCardCatalog Catalog { get; private set; } = null!;
	IMtgApi Api { get; set; } = null!;
	public ICardmarketResolver Resolver { get; private set; } = null!;

	public async Task InitializeAsync()
	{
		Log.Logger = AppLogging.CreateDefaultLoggerConfig().CreateLogger();

		var bundlePath = Path.Combine(AppContext.BaseDirectory, "data", "cards.min.json.gz");
		if (!File.Exists(bundlePath))
		{
			// The bundle is linked from MtgCsvHelper.BlazorWebAssembly/wwwroot/data/cards.min.json.gz
			// via <None CopyToOutputDirectory> in the test csproj. If the link target is missing
			// (fresh checkout, never built the Blazor project), the link copy silently produces nothing.
			throw new FileNotFoundException(
				$"Reference card bundle not found at: {bundlePath}. " +
				$"Source: MtgCsvHelper.BlazorWebAssembly/wwwroot/data/cards.min.json.gz (linked into test output). " +
				$"Generate it with: dotnet run --project tools/MtgCsvHelper.RefreshReferenceData",
				bundlePath);
		}
		await using var fs = File.OpenRead(bundlePath);
		Catalog = await ReferenceCardCatalog.LoadGzipAsync(fs);

		// Note: Api makes live Scryfall calls for catalog-miss cardmarket_ids (the only
		// network path still owned by IMtgApi). Tests that exercise it through the resolver
		// (CardmarketTests with the sample CSV) remain integration-flavored only when the
		// sample ids aren't in the bundle; if they are, the resolver short-circuits.
		Api = new CachedMtgApi();
		Resolver = new CardmarketResolver(Catalog, Api);
	}

	public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition(CatalogCollection.Name)]
public class CatalogCollection : ICollectionFixture<CatalogFixture>
{
	public const string Name = "Catalog";
}
