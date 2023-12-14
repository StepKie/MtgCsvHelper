
using Microsoft.Extensions.DependencyInjection;
using MtgCsvHelper.Services;
using ScryfallApi.Client;

namespace MtgCsvHelper;

public static class ServiceConfiguration
{
	// TODO Noooooooooooooooo
	public static readonly IMtgApi CachedApi = new CachedMtgApi(new ScryfallApiClient(new HttpClient() { BaseAddress = ScryfallApiClientConfig.GetDefault().ScryfallApiBaseAddress }));

	public static IServiceCollection ConfigureMtgCsvHelper(this IServiceCollection services)
	{
		services.AddScryfallApiClient();
		services.AddSingleton<IMtgApi, CachedMtgApi>();

		return services;
	}
}
