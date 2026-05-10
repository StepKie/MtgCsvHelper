using CommandLine;
using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MtgCsvHelper;
using MtgCsvHelper.Services;

// Load the bundled reference catalog up-front so we can register the pre-loaded instance
// into DI (avoids sync-over-async in a factory lambda). Bundle ships next to the exe under data/.
var bundlePath = Path.Combine(AppContext.BaseDirectory, "data", "cards.min.json.gz");
if (!File.Exists(bundlePath))
{
	await Console.Error.WriteLineAsync($"""
		Reference card bundle not found at: {bundlePath}
		Generate it with:
		  dotnet run --project tools/MtgCsvHelper.RefreshReferenceData -- "{bundlePath}"
		""");
	Environment.Exit(1);
}
await using var bundleStream = File.OpenRead(bundlePath);
var catalog = await ReferenceCardCatalog.LoadGzipAsync(bundleStream);

// Load appsettings.json from next to the exe, not from the user's cwd. Without this, running
// the Console from anywhere except its bin output makes config loading silently fail and the
// "supported formats" list ends up empty (which then surfaces as a confusing "Unsupported format"
// error even for known formats).
IHostBuilder builder = Host
	.CreateDefaultBuilder(args)
	.UseContentRoot(AppContext.BaseDirectory)
	.ConfigureServices(services =>
	{
		services.ConfigureMtgCsvHelper();
		services.AddSingleton<IReferenceCardCatalog>(catalog);
	});

using IHost host = builder.Build();

// Ask the service provider for the configuration abstraction.
var config = host.Services.GetRequiredService<IConfiguration>();
Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(config).CreateLogger();

var api = host.Services.GetRequiredService<IMtgApi>();
Log.Information("Loaded reference catalog: {Count:N0} printings.", catalog.Count);

Parser.Default.ParseArguments<CommandLineOptions>(args)
	.WithNotParsed(HandleParseError)
	.WithParsed(RunWithOptions);

Log.Information($"Done. Press Ctrl+C to exit");
Console.ReadLine();

void RunWithOptions(CommandLineOptions opts)
{
	var filesToParse = InputFileResolver.Resolve(opts.InputFilePattern).ToList();
	if (filesToParse.Count == 0)
	{
		Log.Error("No files matched pattern {Pattern} (looked relative to {Cwd}).",
			opts.InputFilePattern, Directory.GetCurrentDirectory());
		return;
	}

	var reader = new MtgCardCsvHandler(catalog, api, config, opts.InputFormat);
	var writer = new MtgCardCsvHandler(catalog, api, config, opts.OutputFormat);

	List<PhysicalMtgCard> cardsFound = [];

	foreach (var fileName in filesToParse)
	{
		try
		{
			var result = reader.ParseCollectionCsv(fileName);
			cardsFound.AddRange(result.Collection.Cards);
			if (result.ErrorCount > 0 || result.WarningCount > 0)
			{
				Log.Information($"{fileName}: {result.Collection.Cards.Count} cards parsed, {result.ErrorCount} errors, {result.WarningCount} warnings");
				foreach (var issue in result.Issues)
				{
					Log.Warning($"  [{issue.Severity}] row {issue.RowNumber}: {issue.Reason}");
				}
			}
		}
		catch (HeaderValidationException ex)
		{
			var missing = ex.InvalidHeaders.SelectMany(h => h.Names).Distinct().ToList();
			Log.Error($"{fileName}: header mismatch — missing required column(s): {string.Join(", ", missing)}. Did you select the correct input format ({opts.InputFormat})?");
		}
	}

	if (cardsFound.Count == 0)
	{
		Log.Warning("No cards parsed from any input file - skipping output write.");
		return;
	}

	writer.WriteCollectionCsv(cardsFound);
}
void HandleParseError(IEnumerable<Error> errs) => Console.WriteLine(string.Join(",", errs)); // TODO: handle errors
