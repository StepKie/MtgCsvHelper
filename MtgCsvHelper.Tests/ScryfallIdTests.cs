using System.Text;

namespace MtgCsvHelper.Tests;

/// <summary>CatalogValidator backfills the resolved Scryfall id onto Printing.Id; writers emit it where the format declares a Scryfall ID column.</summary>
[Collection(CatalogCollection.Name)]
public class ScryfallIdTests(CatalogFixture fixture, ITestOutputHelper output) : ApiBaseTest(fixture, output)
{
	string Write(string format, IList<PhysicalMtgCard> cards)
	{
		var handler = new MtgCardCsvHandler(_catalog, _resolver, _config, format);
		using var stream = new MemoryStream();
		handler.WriteCollectionCsv(cards, stream);

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	IList<PhysicalMtgCard> ReferenceCards(string format)
	{
		var cfg = CardMapFactory.From(_config).First(c => c.Name.Equals(format, StringComparison.OrdinalIgnoreCase));

		return CanonicalReference.LoadCards(_config, _catalog, _resolver, cfg.Currency);
	}

	static PhysicalMtgCard Isperia(IEnumerable<PhysicalMtgCard> cards) =>
		cards.Single(c => c.Printing.Name.StartsWith("Isperia", StringComparison.Ordinal));

	/// <summary>Asserts every ScryfallId-declaring format, not just one — a header typo in any would silently drop the column.</summary>
	[Theory]
	[InlineData("MANABOX")]
	[InlineData("TOPDECKED")]
	[InlineData("DECKBOX")]
	[InlineData("MTGGOLDFISH")]
	[InlineData("ARCHIDEKT")]
	public void ScryfallId_IsCarried_AndEmitted(string format)
	{
		var cards = ReferenceCards(format);
		var id = Isperia(cards).Printing.Id;

		id.Should().NotBe(Guid.Empty, because: "CatalogValidator backfills the resolved printing's Scryfall id");
		Write(format, cards).Should().Contain(id.ToString("D"), because: $"{format} declares a Scryfall ID column");
	}

	[Fact]
	public void ScryfallId_IsNotEmitted_WhenFormatHasNoColumn()
	{
		var cards = ReferenceCards("MOXFIELD");
		var id = Isperia(cards).Printing.Id.ToString("D");

		Write("MOXFIELD", cards).Should().NotContain(id, because: "Moxfield has no Scryfall ID column to emit it into");
	}
}
