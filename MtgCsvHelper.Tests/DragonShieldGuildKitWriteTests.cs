using System.Text;
using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Tests;

/// <summary>
/// Write side of the guild-kit fix: exporting gk1/gk2 cards to Dragon Shield must emit its
/// proprietary <c>GK&lt;n&gt;_&lt;GUILD&gt;</c> codes, since Dragon Shield's importer ignores the
/// canonical codes and name-matches reprints onto the wrong edition. Read side
/// (<c>GK&lt;n&gt;_&lt;GUILD&gt;</c> → gk1/gk2) is covered by <see cref="DragonShieldCodeReadConverterTests"/>.
/// </summary>
[Collection(CatalogCollection.Name)]
public class DragonShieldGuildKitWriteTests(CatalogFixture fixture, ITestOutputHelper output) : ApiBaseTest(fixture, output)
{
	static PhysicalMtgCard Card(string name, string set, string collectorNumber) => new()
	{
		Count = 1,
		Condition = CardCondition.NearMint,
		Finish = CardFinish.Normal,
		Language = "en",
		Printing = new Card { Name = name, Set = set, SetName = "", CollectorNumber = collectorNumber },
	};

	string WriteDragonShield(params PhysicalMtgCard[] cards)
	{
		var handler = new MtgCardCsvHandler(_catalog, _resolver, _config, "DRAGONSHIELD");
		using var stream = new MemoryStream();
		handler.WriteCollectionCsv(cards, stream);

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	[Theory]
	[InlineData("Isperia, Supreme Judge", "GK2", "1", "GK2_AZORIU")]
	[InlineData("Azorius Herald", "GK2", "2", "GK2_AZORIU")]
	[InlineData("Belfry Spirit", "GK2", "29", "GK2_ORZHOV")]
	[InlineData("Cloudfin Raptor", "GK2", "108", "GK2_SIMIC")]
	[InlineData("Etrata, the Silencer", "GK1", "1", "GK1_DIMIR")]
	[InlineData("Plains", "GK1", "100", "GK1_BOROS")]
	public void GuildKitCard_EmitsNativeDragonShieldCode(string name, string set, string collectorNumber, string expectedCode)
	{
		var csv = WriteDragonShield(Card(name, set, collectorNumber));

		// Set codes never contain commas, so the native code surrounded by delimiters is an unambiguous match.
		csv.Should().Contain($",{expectedCode},");
	}

	[Fact]
	public void NonGuildKitCard_EmitsCanonicalCode()
	{
		var csv = WriteDragonShield(Card("Lightning Bolt", "M11", "149"));

		csv.Should().Contain(",M11,");
	}

	[Fact]
	public void WatermarkLessGuildKitCard_FallsBackToCanonical()
	{
		// Birds of Paradise (gk2 #82) has no Scryfall watermark, so it falls back to the canonical code.
		var csv = WriteDragonShield(Card("Birds of Paradise", "GK2", "82"));

		csv.Should().Contain(",GK2,").And.NotContain("GK2_");
	}

	[Fact]
	public void NativeCode_RoundTripsBackToCanonical()
	{
		var handler = new MtgCardCsvHandler(_catalog, _resolver, _config, "DRAGONSHIELD");
		using var stream = new MemoryStream();
		handler.WriteCollectionCsv([Card("Isperia, Supreme Judge", "GK2", "1")], stream);
		stream.Position = 0;

		var parsed = handler.ParseCollectionCsv(stream);

		parsed.Issues.Should().NotContain(i => i.Severity == IssueSeverity.Error);
		parsed.Collection.Cards.Should().ContainSingle()
			.Which.Printing.Set.Should().Be("GK2", because: "the native GK2_AZORIU code collapses back to the canonical Scryfall set on read");
	}
}
