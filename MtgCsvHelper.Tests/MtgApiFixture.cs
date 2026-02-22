using MtgCsvHelper.Services;
using ScryfallApi.Client;
using Serilog;

namespace MtgCsvHelper.Tests;

public class MtgApiFixture : IAsyncLifetime
{
	public IMtgApi Api { get; private set; } = null!;

	public async Task InitializeAsync()
	{
		Log.Logger = AppLogging.GetDefaultLoggerConfig.CreateLogger();
		Api = new CachedMtgApi(new ScryfallApiClient(CachedMtgApi.DEFAULT_CLIENT));
		await Api.LoadData();
	}

	// HttpClient disposal intentionally skipped — acceptable for test fixtures
	public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition(MtgApiCollection.Name)]
public class MtgApiCollection : ICollectionFixture<MtgApiFixture>
{
	public const string Name = "MtgApi";
}
