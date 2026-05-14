using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MtgCsvHelper;
using MtgCsvHelper.BlazorWebAssembly;
using MudBlazor.Services;
using Serilog;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
	.ConfigureMtgCsvHelper()
	.AddMudServices(c =>
	{
		c.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
		c.SnackbarConfiguration.VisibleStateDuration = 4000;
	})
	// All Singleton — Blazor WASM has a single root scope per session, and keeping the
	// catalog loader / its captured HttpClient / the catalog-factory all at the same
	// (long-lived) lifetime avoids any captive-dependency surprises if this DI graph is
	// ever ported to a Blazor Server or hybrid project.
	.AddSingleton<ICatalogLoader, CatalogLoader>()
	.AddSingleton<Func<IReferenceCardCatalog>>(sp =>
		() => sp.GetRequiredService<ICatalogLoader>().Catalog
			?? throw new InvalidOperationException("Reference card catalog is not loaded yet."))
	.AddSingleton(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
Log.Information("Hello, Blazor, Serilog online!");
builder.Logging.AddSerilog();

// Catalog load happens lazily after the shell renders — kicked off from MainLayout, owned by
// CatalogLoader. See ICatalogLoader for status / progress / error wiring.

await builder.Build().RunAsync();
