using Microsoft.Extensions.DependencyInjection;
using MtgCsvHelper.Services;
using ScryfallApi.Client;

namespace MtgCsvHelper;

public static class ServiceConfiguration
{
	public static IServiceCollection ConfigureMtgCsvHelper(this IServiceCollection services)
	{
		// Before, we used default method services.AddScryfallApiClient() instead of injecting our own manually configured client.
		// However, this no longer works since the wrapped client does not set the required DefaultRequestHeaders, and the Scryfall API now requires them.
		// ScryfallApiClientConfig parameter also provides no access to DefaultRequestHeaders.
		// services.AddScryfallApiClient();

		services.AddHttpClient<ScryfallApiClient>(client =>
		{
			client.BaseAddress = new Uri("https://api.scryfall.com/");
			client.DefaultRequestHeaders.Add("User-Agent", "MtgCsvHelper/1.0.0");
			client.DefaultRequestHeaders.Add("Accept", "application/json");

		});

		services.AddSingleton(ScryfallApiClientConfig.GetDefault());
		services.AddSingleton<IMtgApi, CachedMtgApi>();
		return services;
	}
}
