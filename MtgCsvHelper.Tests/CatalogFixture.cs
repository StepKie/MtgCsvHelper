using MtgCsvHelper.Services;
using Serilog;

namespace MtgCsvHelper.Tests;

/// <summary>
/// Loads the bundled reference catalog once for the entire test run, plus a fresh
/// <see cref="IMtgApi"/> for the cardmarket-fallback path. Tests that don't need either
/// can stick with <see cref="BaseTest"/>; tests that touch handlers or the catalog
/// extend <see cref="ApiBaseTest"/>.
/// </summary>
public class CatalogFixture : IAsyncLifetime
{
	public IReferenceCardCatalog Catalog { get; private set; } = null!;
	public IMtgApi Api { get; private set; } = null!;

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

		Api = new CachedMtgApi();
	}

	public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition(CatalogCollection.Name)]
public class CatalogCollection : ICollectionFixture<CatalogFixture>
{
	public const string Name = "Catalog";
}
