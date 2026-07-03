using System.Text;

namespace MtgCsvHelper.Tests;

/// <summary>CatalogValidator resolves a row by its Scryfall id when present (correcting a reshaped (set, #)),
/// backfills the resolved id onto Printing.Id, and writers emit it where the format declares a Scryfall ID column.</summary>
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

	[Fact]
	public void ScryfallId_OverridesReshapedCoordinate_OnRead()
	{
		// (set, #) lea #13 is Circle of Protection: White, not Demonic Tutor; the id pins the real lea #104.
		const string csv = """
			QUANTITY,"NAME",SETCODE,"SETNAME","COLLECTOR NUMBER",FINISH,PRICE,RARITY,ID,ACQUIRED DATE,ACQUIRED PRICE,LANG,PRICE SALE,SIGNING,ALTERATION,CONDITION,NOTES,TAGS
			1,"Demonic Tutor",lea,"Limited Edition Alpha",13,nonfoil,,uncommon,711d4d54-5520-4de8-9b93-79902ed8e562,2023-01-01T00:00:00.000Z,,en,,,false,near mint
			""";
		var handler = new MtgCardCsvHandler(_catalog, _resolver, _config, "TOPDECKED");
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

		var result = handler.ParseCollectionCsv(stream);

		result.ErrorCount.Should().Be(0, because: "the Scryfall id resolves even though (set, #) names a different card");
		var card = result.Collection.Cards.Should().ContainSingle().Subject;
		card.Printing.Name.Should().Be("Demonic Tutor");
		card.Printing.Set.Should().Be("LEA", because: "the id rewrites the full coordinate from the catalog");
		card.Printing.CollectorNumber.Should().Be("104", because: "the id pins lea #104, correcting the reshaped #13 coordinate");
	}
}
