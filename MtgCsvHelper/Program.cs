using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using MtgCsvHelper;
using Serilog;
using CsvHelper.Configuration;

using IHost host = Host.CreateDefaultBuilder(args).Build();

// Ask the service provider for the configuration abstraction.
IConfiguration config = host.Services.GetRequiredService<IConfiguration>();


Log.Logger = new LoggerConfiguration()
		.ReadFrom.Configuration(config)
		.CreateLogger();

// Get values from the config given their key and their target type.
string sampleValue = config.GetValue<string>("Editions:Media Promos");


// Application code
string dsCsvFile = $"SampleCsvs/all-folders.csv";
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
    _ = stream ?? throw new FileNotFoundException($"{csvFilePath} not found");

    using CsvReader csv = new(stream, CultureInfo.InvariantCulture);
    csv.Read(); // Read twice to discard "=sep" in DragonShield
    csv.Read();
	_ = csv.ReadHeader();
	csv.Context.RegisterClassMap<DeckboxMap>();
	string[] headerRecord = csv.HeaderRecord!;


    List<PhysicalMtgCard> autoReadCards = csv.GetRecords<PhysicalMtgCard>().ToList();

    return autoReadCards;
    List<PhysicalMtgCard> cards = new();

	// Folder Name,Quantity,Trade Quantity,Card Name,Set Code,Set Name,Card Number,Condition,Printing,Language,Price Bought,Date Bought,LOW,MID,MARKET
	while (csv.Read())
    {
        try
        {
            PhysicalMtgCard card = new()
            {
                Printing = new Printing()
                {
                    Card = new MtgCard { Name = csv.GetField("Card Name") },
                    Set = new()
                    {
                        Id = csv.GetField("Set Code"),
                        FullName = csv.GetField("Set Name"),
                    },
                    IdInSet = csv.GetField("Card Number"),
                },
                Condition = CardCondition.FromDisplayName<CardCondition>(csv.GetField("Condition")),
                Foil = csv.GetField("Printing").Equals("Foil"),
                Language = csv.GetField("Language"),
                PriceBought = csv.GetField<double>("Price Bought"),
                DateBought = DateTime.Parse(csv.GetField("Date Bought")),
                // Folder Name,Quantity,Trade Quantity,Card Name,Set Code,Set Name,Card Number,Condition,Printing,Language,Price Bought,Date Bought,LOW,MID,MARKET


            };
            cards.Add(card);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error while reading row {csv.Parser.RawRecord}");
        }
    }

    return cards;
}

//Count,Tradelist Count, Name, Edition, Card Number, Condition, Language, Foil, Signed, Artist Proof, Altered Art, Misprint, Promo, Textless, My Price

/// <summary> TODO Make configurable with target format (Deckbox, DragonShield etc.) </summary>
void WriteCollectionCsv(IList<PhysicalMtgCard> cards)
{
	using (var writer = new StreamWriter("deckboxoutput.csv"))
	using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
	{
		csv.Context.RegisterClassMap<DeckboxMap>();
		csv.WriteHeader<PhysicalMtgCard>();
        csv.Flush();

		csv.NextRecord();
		foreach (var card in cards)
		{
			csv.WriteRecord(card);
            csv.Flush();
			csv.NextRecord();
		}
	}
}
