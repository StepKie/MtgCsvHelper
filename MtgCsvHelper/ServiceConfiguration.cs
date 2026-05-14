using Microsoft.Extensions.DependencyInjection;
using MtgCsvHelper.Services;

namespace MtgCsvHelper;

public static class ServiceConfiguration
{
	/// <summary>
	/// Registers the always-available <see cref="IMtgApi"/> and <see cref="ICardmarketResolver"/>
	/// singletons.
	/// </summary>
	/// <remarks>
	/// Caller must <b>also</b> register a <c>Func&lt;IReferenceCardCatalog&gt;</c> factory so
	/// catalog-dependent services can look up the catalog at use time (rather than at
	/// resolver-construction time). This indirection lets the Blazor app defer catalog loading
	/// to after the shell renders without making every consumer Scoped.
	/// Typical pattern:
	/// <list type="bullet">
	///   <item>Console (eager load): <c>services.AddSingleton&lt;Func&lt;IReferenceCardCatalog&gt;&gt;(_ =&gt; () =&gt; catalog)</c></item>
	///   <item>Blazor (background load): factory returns <c>loader.Catalog ?? throw</c></item>
	/// </list>
	/// </remarks>
	public static IServiceCollection ConfigureMtgCsvHelper(this IServiceCollection services)
	{
		services.AddSingleton<IMtgApi, CachedMtgApi>();
		services.AddSingleton<ICardmarketResolver, CardmarketResolver>();

		return services;
	}
}
