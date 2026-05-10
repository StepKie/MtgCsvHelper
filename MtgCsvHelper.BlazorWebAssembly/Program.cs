using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MtgCsvHelper;
using MtgCsvHelper.BlazorWebAssembly;
using Serilog;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
	.ConfigureMtgCsvHelper()
	.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
Log.Information("Hello, Blazor, Serilog online!");
builder.Logging.AddSerilog();

// Load the reference-card bundle once at startup, before DI is built. The bundle is published
// alongside the static assets at wwwroot/data/cards.min.json.gz; it's regenerated in CI before
// `dotnet publish` via the RefreshReferenceData tool.
try
{
	using var startupClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
	await using var bundleStream = await startupClient.GetStreamAsync("data/cards.min.json.gz");
	var catalog = await ReferenceCardCatalog.LoadGzipAsync(bundleStream);
	Log.Information("Loaded reference catalog: {Count:N0} printings.", catalog.Count);
	builder.Services.AddSingleton<IReferenceCardCatalog>(catalog);
}
catch (Exception ex)
{
	// Without the catalog the app is non-functional. Surface the failure to the user
	// instead of letting the page render blank with only a console-log error.
	Log.Fatal(ex, "Failed to load reference card bundle from data/cards.min.json.gz");
	await Console.Error.WriteLineAsync($"Failed to load reference card bundle: {ex.Message}");
	throw;
}

await builder.Build().RunAsync();
