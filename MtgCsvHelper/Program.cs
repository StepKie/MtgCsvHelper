using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using MtgCsvHelper;
using Serilog;
using MtgCsvHelper.Models;

var builder = Host.CreateDefaultBuilder(args);
using IHost host = Host.CreateDefaultBuilder(args).Build();

// Ask the service provider for the configuration abstraction.
IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

Log.Logger = new LoggerConfiguration()
		.ReadFrom.Configuration(config)
		.CreateLogger();

// Application code

// Set ConfigFile through singleton (due to how ClassMaps are registered)
CsvToCardMap.ConfigFile = config;
string dsCsvFile = $"SampleCsvs/dragonshield-collection_2022-11-11.csv";
Log.Information("Parsing Csv ...");
var cards = ParseCollectionCsv(dsCsvFile);
Log.Information($"Found {cards.Count} cards");

WriteCollectionCsv(cards);
Log.Information($"Done. Press Ctrl+C to exit");

await host.RunAsync();


/// <summary> TODO Specify or auto-detect format, currently hardcoded to DragonShield </summary>
IList<PhysicalMtgCard> ParseCollectionCsv(string csvFilePath)
{
    using StreamReader stream = new(csvFilePath);
    TextReader r = new StringReader("");
    _ = stream ?? throw new FileNotFoundException($"{csvFilePath} not found");

    using CsvReader csv = new(stream, CultureInfo.InvariantCulture);
    csv.Read(); // Read twice to discard "=sep" in DragonShield
    csv.Read();
	
	_ = csv.ReadHeader();
	csv.Context.RegisterClassMap<DragonShieldMap>();
	string[] headerRecord = csv.HeaderRecord!;


    List<PhysicalMtgCard> autoReadCards = csv.GetRecords<PhysicalMtgCard>().ToList();

    return autoReadCards;
}

//Count,Tradelist Count, Name, Edition, Card Number, Condition, Language, Foil, Signed, Artist Proof, Altered Art, Misprint, Promo, Textless, My Price

/// <summary> TODO Make configurable with target format (Deckbox, DragonShield etc.) </summary>
void WriteCollectionCsv(IList<PhysicalMtgCard> cards)
{
	using var writer = new StreamWriter("moxfieldoutput.csv");
	using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

	csv.Context.RegisterClassMap<MoxfieldMap>();
	
	csv.WriteHeader<PhysicalMtgCard>();
	csv.NextRecord();
	csv.WriteRecords(cards);
	csv.Flush();
}
