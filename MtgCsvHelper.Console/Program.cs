using CommandLine;
using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MtgCsvHelper;
using MtgCsvHelper.Services;

IHostBuilder builder = Host
	.CreateDefaultBuilder(args)
	.ConfigureServices(builder => builder.ConfigureMtgCsvHelper());

using IHost host = builder.Build();

// Ask the service provider for the configuration abstraction.
var config = host.Services.GetRequiredService<IConfiguration>();
Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(config).CreateLogger();

var api = host.Services.GetService<IMtgApi>()!;
await api.LoadData();

Parser.Default.ParseArguments<CommandLineOptions>(args)
	.WithNotParsed(HandleParseError)
	.WithParsed(RunWithOptions);

Log.Information($"Done. Press Ctrl+C to exit");
Console.ReadLine();

void RunWithOptions(CommandLineOptions opts)
{
	var filesToParse = Directory.GetFiles(Directory.GetCurrentDirectory(), opts.InputFilePattern);

	var reader = new MtgCardCsvHandler(api, config, opts.InputFormat);
	var writer = new MtgCardCsvHandler(api, config, opts.OutputFormat);

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
