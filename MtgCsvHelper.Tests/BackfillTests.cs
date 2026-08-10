using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Tests;

/// <summary>
/// Catalog-stamped metadata beyond the Scryfall id (covered by <see cref="ScryfallIdTests"/>):
/// CatalogValidator fills Rarity, MultiverseIds, and TcgplayerId when a printing resolves, and
/// formats declaring the column emit them like any other field. Lightning Bolt M11 #149 is a
/// stable common printing.
/// </summary>
[Collection(CatalogCollection.Name)]
public class BackfillTests(CatalogFixture fixture, ITestOutputHelper output) : ApiBaseTest(fixture, output)
{
	[Fact]
	public void CatalogValidator_StampsIdsAndRarity_OnResolve()
	{
		var reference = _catalog.FindBySetAndCollectorNumber("M11", "149")!;
		var card = ParsedLightningBolt();

		card.Printing.MultiverseIds.Should().Equal(reference.MultiverseIds!);
		card.Printing.TcgplayerId.Should().Be(reference.TcgplayerId!.Value);
		card.Rarity.Should().Be(CardRarity.Common);
	}

	[Fact]
	public void Archidekt_EmitsMultiverseId()
	{
		var reference = _catalog.FindBySetAndCollectorNumber("M11", "149")!;
		var row = WriteRow("ARCHIDEKT", ParsedLightningBolt());

		row["Multiverse Id"].Should().Be(reference.MultiverseIds!.First().ToString());
	}

	[Fact]
	public void Manabox_EmitsLowercaseRarity()
	{
		var row = WriteRow("MANABOX", ParsedLightningBolt());

		row["Rarity"].Should().Be("common");
	}

	[Fact]
	public void Tcgplayer_EmitsTitleCaseRarityAndProductId()
	{
		var reference = _catalog.FindBySetAndCollectorNumber("M11", "149")!;
		var row = WriteRow("TCGPLAYER", ParsedLightningBolt());

		row["Rarity"].Should().Be("Common");
		row["Product ID"].Should().Be(reference.TcgplayerId!.Value.ToString());
	}

	// TCGplayer lists etched printings as a separate product; CMM #509 is Demonic Tutor's etched-only printing.
	[Fact]
	public void Tcgplayer_EtchedFinish_EmitsEtchedProductId()
	{
		var reference = _catalog.FindBySetAndCollectorNumber("CMM", "509")!;
		var row = WriteRow("TCGPLAYER", ParsedCard("1,Demonic Tutor,CMM,509,etched"));

		row["Product ID"].Should().Be(reference.TcgplayerEtchedId!.Value.ToString());
	}

	// A native column with no catalog source stays blank — we emit only what the model carries.
	[Fact]
	public void NonDerivableNativeColumn_StaysBlank()
	{
		var row = WriteRow("MANABOX", ParsedLightningBolt());

		row["ManaBox ID"].Should().BeEmpty();
	}

	// A card that never went through the pipeline carries no stamps; its metadata columns stay blank.
	[Fact]
	public void UnstampedCard_EmitsBlankMetadataColumns()
	{
		var unstamped = new PhysicalMtgCard
		{
			Count = 1,
			Printing = new Card { Name = "Lightning Bolt", Set = "M11", SetName = "Magic 2011", CollectorNumber = "149" },
		};
		var row = WriteRow("ARCHIDEKT", unstamped);

		row["Scryfall ID"].Should().BeEmpty();
		row["Multiverse Id"].Should().BeEmpty();
	}

	PhysicalMtgCard ParsedLightningBolt() => ParsedCard("1,Lightning Bolt,M11,149,");

	// Parses a minimal Moxfield row so the enrichment pipeline resolves and stamps the printing.
	PhysicalMtgCard ParsedCard(string row)
	{
		var csv = "Count,Name,Edition,Collector Number,Foil\n" + row + "\n";
		var handler = new MtgCardCsvHandler(_catalog, _resolver, _config, "MOXFIELD");
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

		return handler.ParseCollectionCsv(stream).Collection.Cards.Single();
	}

	// Writes a single card and returns the emitted row keyed by native header.
	Dictionary<string, string> WriteRow(string format, PhysicalMtgCard card)
	{
		var handler = new MtgCardCsvHandler(_catalog, _resolver, _config, format);
		using var stream = new MemoryStream();
		handler.WriteCollectionCsv([card], stream);
		stream.Position = 0;

		using var csv = new CsvReader(new StreamReader(stream), new CsvConfiguration(CultureInfo.InvariantCulture));
		csv.Read();
		csv.ReadHeader();
		csv.Read();

		return csv.HeaderRecord!.ToDictionary(header => header, header => csv.GetField(header) ?? string.Empty);
	}
}
