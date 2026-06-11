using System.Text;
using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Tests;

/// <summary>
/// Write side of the guild-kit fix: DragonShield resolves imports by Set <em>Name</em>, so exporting
/// gk1/gk2 cards must emit the native per-guild edition (<c>Guild Kit: Azorius</c>, …) rather than the
/// canonical <c>RNA Guild Kit</c>, which DragonShield name-matches onto the wrong edition. Read side
/// (the <c>GK&lt;n&gt;_&lt;GUILD&gt;</c> set codes DragonShield exports) is covered by <see cref="DragonShieldCodeReadConverterTests"/>.
/// </summary>
[Collection(CatalogCollection.Name)]
public class DragonShieldGuildKitWriteTests(CatalogFixture fixture, ITestOutputHelper output) : ApiBaseTest(fixture, output)
{
	static PhysicalMtgCard Card(string name, string set, string collectorNumber, string setName = "") => new()
	{
		Count = 1,
		Condition = CardCondition.NearMint,
		Finish = CardFinish.Normal,
		Language = "en",
		Printing = new Card { Name = name, Set = set, SetName = setName, CollectorNumber = collectorNumber },
	};

	string WriteDragonShield(params PhysicalMtgCard[] cards)
	{
		var handler = new MtgCardCsvHandler(_catalog, _resolver, _config, "DRAGONSHIELD");
		using var stream = new MemoryStream();
		handler.WriteCollectionCsv(cards, stream);

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	[Theory]
	[InlineData("Isperia, Supreme Judge", "GK2", "1", "Guild Kit: Azorius")]
	[InlineData("Azorius Herald", "GK2", "2", "Guild Kit: Azorius")]
	[InlineData("Belfry Spirit", "GK2", "29", "Guild Kit: Orzhov")]
	[InlineData("Cloudfin Raptor", "GK2", "108", "Guild Kit: Simic")]
	[InlineData("Etrata, the Silencer", "GK1", "1", "Guild Kit: Dimir")]
	[InlineData("Plains", "GK1", "100", "Guild Kit: Boros")]
	public void GuildKitCard_EmitsNativeEditionName(string name, string set, string collectorNumber, string expectedEdition)
	{
		var csv = WriteDragonShield(Card(name, set, collectorNumber));

		// Guild edition names contain no commas, so the edition surrounded by delimiters is an unambiguous match.
		csv.Should().Contain($",{expectedEdition},");
	}

	[Fact]
	public void NonGuildKitCard_KeepsCanonicalSetName()
	{
		var csv = WriteDragonShield(Card("Lightning Bolt", "M11", "149", setName: "Magic 2011"));

		csv.Should().Contain(",Magic 2011,").And.NotContain("Guild Kit:");
	}

	[Fact]
	public void WatermarkLessGuildKitCard_KeepsCanonicalSetName()
	{
		// Birds of Paradise (gk2 #82) has no Scryfall watermark, so it isn't in the editions table and keeps its set name.
		var csv = WriteDragonShield(Card("Birds of Paradise", "GK2", "82", setName: "RNA Guild Kit"));

		csv.Should().Contain(",RNA Guild Kit,").And.NotContain("Guild Kit: ");
	}

	[Fact]
	public void NativeEdition_RoundTripsBackToCanonicalSet()
	{
		var handler = new MtgCardCsvHandler(_catalog, _resolver, _config, "DRAGONSHIELD");
		using var stream = new MemoryStream();
		handler.WriteCollectionCsv([Card("Isperia, Supreme Judge", "GK2", "1")], stream);
		stream.Position = 0;

		var parsed = handler.ParseCollectionCsv(stream);

		parsed.Issues.Should().NotContain(i => i.Severity == IssueSeverity.Error);
		parsed.Collection.Cards.Should().ContainSingle()
			.Which.Printing.Set.Should().Be("GK2", because: "the native edition name resolves on read; Set Code GK2 is unchanged (only GK{n}_* codes are converted)");
	}
}
