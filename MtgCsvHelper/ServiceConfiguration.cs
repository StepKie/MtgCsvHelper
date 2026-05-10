using Microsoft.Extensions.DependencyInjection;
using MtgCsvHelper.Services;

namespace MtgCsvHelper;

public static class ServiceConfiguration
{
	public static IServiceCollection ConfigureMtgCsvHelper(this IServiceCollection services)
	{
		services.AddSingleton<IMtgApi, CachedMtgApi>();
		return services;
	}
}
