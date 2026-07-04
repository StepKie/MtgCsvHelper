using CommandLine;
using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MtgCsvHelper;
using MtgCsvHelper.Services;

// Pre-load the bundled catalog so DI registers the instance directly (no sync-over-async factory lambda).
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

// Load appsettings.json from next to the exe — from any other cwd, config silently loads empty and every format reads as unsupported.
IHostBuilder builder = Host
	.CreateDefaultBuilder(args)
	.UseContentRoot(AppContext.BaseDirectory)
	.ConfigureServices(services =>
	{
		services.ConfigureMtgCsvHelper();
		services.AddSingleton<IReferenceCardCatalog>(catalog);
		// Console loads the catalog eagerly above; factory returns the captured instance.
		services.AddSingleton<Func<IReferenceCardCatalog>>(_ => () => catalog);
	});

using IHost host = builder.Build();

var config = host.Services.GetRequiredService<IConfiguration>();

// Console + File sinks layer on the shared AppLogging base; kept in code so the shared appsettings can link into Blazor.
Log.Logger = AppLogging.CreateDefaultLoggerConfig()
	.Enrich.FromLogContext()
	.WriteTo.Console(outputTemplate: AppLogging.DEFAULT_OUTPUT_TEMPLATE)
	.WriteTo.File("Logs/log.txt", outputTemplate: AppLogging.DEFAULT_OUTPUT_TEMPLATE)
	.CreateLogger();

var resolver = host.Services.GetRequiredService<ICardmarketResolver>();
Log.Information("Loaded reference catalog: {Count:N0} printings.", catalog.Count);

Parser.Default.ParseArguments<CommandLineOptions>(args)
	.WithNotParsed(HandleParseError)
	.WithParsed(RunWithOptions);

Log.Information("Done.");

void RunWithOptions(CommandLineOptions opts)
{
	var filesToParse = InputFileResolver.Resolve(opts.InputFilePattern).ToList();
	if (filesToParse.Count == 0)
	{
		Log.Error("No files matched pattern {Pattern} (looked relative to {Cwd}).",
			opts.InputFilePattern, Directory.GetCurrentDirectory());
		return;
	}

	var detector = new FormatDetector([.. CardMapFactory.SupportedConfigs(config)]);
	var writer = new MtgCardCsvHandler(catalog, resolver, config, opts.OutputFormat);

	List<PhysicalMtgCard> cardsFound = [];

	foreach (var fileName in filesToParse)
	{
		string? inputFormat = opts.InputFormat;
		if (inputFormat is null)
		{
			using var detectStream = File.OpenRead(fileName);
			inputFormat = detector.Detect(detectStream);
		}
		if (inputFormat is null)
		{
			Log.Error("{FileName}: couldn't auto-detect format from CSV headers. Specify --in explicitly.", fileName);
			continue;
		}
		// opts.InputFormat null at this point means we fell through to auto-detect.
		if (opts.InputFormat is null)
		{
			Log.Information("{FileName}: auto-detected format {InputFormat}.", fileName, inputFormat);
		}

		var reader = new MtgCardCsvHandler(catalog, resolver, config, inputFormat);
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
			Log.Error($"{fileName}: header mismatch — missing required column(s): {string.Join(", ", ex.MissingColumns())}. Did you select the correct input format ({inputFormat})?");
		}
	}

	if (cardsFound.Count == 0)
	{
		Log.Warning("No cards parsed from any input file - skipping output write.");
		return;
	}

	writer.WriteCollectionCsv(cardsFound);
}
// Parser.Default already printed usage/help to stderr; our job is just the exit code (0 for --help/--version, 1 for real parse errors so CI fails).
void HandleParseError(IEnumerable<Error> errs) => Environment.Exit(errs.All(e => e.Tag is ErrorType.HelpRequestedError or ErrorType.VersionRequestedError) ? 0 : 1);
