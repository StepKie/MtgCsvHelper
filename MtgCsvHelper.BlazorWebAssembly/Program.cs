using System.Runtime.InteropServices.JavaScript;
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
	.AddMudServices()
	.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
Log.Information("Hello, Blazor, Serilog online!");
builder.Logging.AddSerilog();

// Load the reference-card bundle once at startup, before DI is built. The bundle is published
// alongside the static assets at wwwroot/data/cards.min.json.gz; it's regenerated in CI before
// `dotnet publish` via the RefreshReferenceData tool.
try
{
	StartupInterop.SetLoadingStatus("Loading card data…");
	using var startupClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
	await using var bundleStream = await startupClient.GetStreamAsync("data/cards.min.json.gz");
	var catalog = await ReferenceCardCatalog.LoadGzipAsync(bundleStream);
	Log.Information("Loaded reference catalog: {Count:N0} printings.", catalog.Count);
	builder.Services.AddSingleton<IReferenceCardCatalog>(catalog);
}
catch (Exception ex)
{
	// Without the catalog the app is non-functional. Render an in-page error UI via
	// the pre-Blazor JS hook in index.html, then rethrow to halt WASM so Blazor
	// doesn't boot and overwrite the message.
	Log.Fatal(ex, "Failed to load reference card bundle from data/cards.min.json.gz");
	await Console.Error.WriteLineAsync($"Failed to load reference card bundle: {ex.Message}");
	try
	{
		StartupInterop.ShowStartupError(
			"Couldn't load card data",
			"The reference card bundle failed to load, so the app can't start. Check your connection and try again.",
			ex.ToString());
	}
	catch (Exception jsEx)
	{
		// JS interop itself failed — fall back to console only.
		await Console.Error.WriteLineAsync($"Also failed to render error UI: {jsEx}");
	}
	throw;
}

await builder.Build().RunAsync();

internal partial class StartupInterop
{
	[JSImport("globalThis.mtgShowStartupError")]
	internal static partial void ShowStartupError(string title, string message, string detail);

	[JSImport("globalThis.mtgSetLoadingStatus")]
	internal static partial void SetLoadingStatus(string message);
}
