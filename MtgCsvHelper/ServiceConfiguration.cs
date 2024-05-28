using Microsoft.Extensions.DependencyInjection;
using MtgCsvHelper.Services;
using ScryfallApi.Client;

namespace MtgCsvHelper;

public static class ServiceConfiguration
{
	public static IServiceCollection ConfigureMtgCsvHelper(this IServiceCollection services)
	{
		services.AddScryfallApiClient();
		services.AddSingleton(ScryfallApiClientConfig.GetDefault());
		services.AddSingleton<IMtgApi, CachedMtgApi>();
		return services;
	}
}
