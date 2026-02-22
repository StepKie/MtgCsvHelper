using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MtgCsvHelper;
using MtgCsvHelper.BlazorWebAssembly;
using MtgCsvHelper.Services;
using Serilog;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Configuration.AddJsonFile("appsettings.json", optional: false);

builder.Services
	.ConfigureMtgCsvHelper()
	.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
	.AddLogging(builder => builder.AddSerilog())
	.BuildServiceProvider();

var host = builder.Build();
var config = builder.Configuration;

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(config).CreateLogger();
// TODO Figure out why Serilog does not want to work
Log.Information("Hello, Blazor, Serilog online!");

var api = host.Services.GetService<IMtgApi>()!;
await api.LoadData();

await host.RunAsync();
