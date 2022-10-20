using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;

using IHost host = Host.CreateDefaultBuilder(args).Build();

// Ask the service provider for the configuration abstraction.
IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

// Get values from the config given their key and their target type.
string sampleValue = config.GetValue<string>("Editions:Media Promos");


// Application code
string dsCsvFile = $"SampleCsvs/all-folders.csv";
ParseMtg();

await host.RunAsync();


IList<MtgCard> ParseMtg()
{
    using StreamReader stream = new(dsCsvFile);
    _ = stream ?? throw new FileNotFoundException($"{dsCsvFile} not found");

    using CsvReader csv = new(stream, CultureInfo.InvariantCulture);
    _ = csv.Read(); // Read twice to discard "=sep" in DragonShield
    _ = csv.Read();
    _ = csv.ReadHeader();
    string[] headerRecord = csv.HeaderRecord!;
   

    List<MtgCard> cards = new();

    while (csv.Read())
    {
        MtgCard record = new()
        {
            Name = csv.GetField("Card Name"),
          
        };
        cards.Add(record);
    }

    return cards;
}
