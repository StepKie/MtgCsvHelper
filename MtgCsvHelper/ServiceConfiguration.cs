using Microsoft.Extensions.DependencyInjection;
using MtgCsvHelper.Services;

namespace MtgCsvHelper;

public static class ServiceConfiguration
{
	/// <summary>
	/// Registers the always-available <see cref="IMtgApi"/> singleton.
	/// </summary>
	/// <remarks>
	/// Caller must <b>also</b> register <see cref="IReferenceCardCatalog"/> before resolving
	/// catalog-dependent services. Loading is platform-specific (file vs HTTP) and async, so
	/// it can't live inside this synchronous extension. Typical pattern: pre-load the bundle,
	/// then <c>services.AddSingleton&lt;IReferenceCardCatalog&gt;(catalog)</c>.
	/// </remarks>
	public static IServiceCollection ConfigureMtgCsvHelper(this IServiceCollection services)
	{
		services.AddSingleton<IMtgApi, CachedMtgApi>();
		return services;
	}
}
