using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace MtgCsvHelper.Tests;

public class ReferenceCardCatalogTests
{
	// Hand-crafted fixture covering the variations the catalog needs to handle.
	static readonly ReferenceCard LightningBoltM11 = new(
		Id: Guid.Parse("d573ef03-4730-45aa-93dd-e45ac1dbaf4a"),
		OracleId: Guid.Parse("4457ed35-7c10-48c8-9776-456485fdf070"),
		Name: "Lightning Bolt",
		Set: "M11",
		SetName: "Magic 2011",
		CollectorNumber: "149",
		Lang: "en",
		Layout: "normal",
		Finishes: ["nonfoil", "foil"],
		FrameEffects: null,
		BorderColor: "black",
		PromoTypes: null,
		CardmarketId: 5395,
		TcgplayerId: 1174,
		TcgplayerEtchedId: null,
		MultiverseIds: [209]);

	static readonly ReferenceCard DelverOfSecretsTransform = new(
		Id: Guid.NewGuid(),
		OracleId: Guid.NewGuid(),
		Name: "Delver of Secrets // Insectile Aberration",
		Set: "ISD",
		SetName: "Innistrad",
		CollectorNumber: "51",
		Lang: "en",
		Layout: "transform",
		Finishes: ["nonfoil", "foil"],
		FrameEffects: null,
		BorderColor: "black",
		PromoTypes: null,
		CardmarketId: 99001,
		TcgplayerId: null,
		TcgplayerEtchedId: null,
		MultiverseIds: null);

	static readonly ReferenceCard ClueToken = new(
		Id: Guid.NewGuid(),
		OracleId: Guid.NewGuid(),
		Name: "Clue",
		Set: "TMH2",
		SetName: "Modern Horizons 2 Tokens",
		CollectorNumber: "14",
		Lang: "en",
		Layout: "token",
		Finishes: ["nonfoil"],
		FrameEffects: null,
		BorderColor: "black",
		PromoTypes: null,
		CardmarketId: null,
		TcgplayerId: null,
		TcgplayerEtchedId: null,
		MultiverseIds: null);

	static readonly ReferenceCard CommitMemorySplit = new(
		Id: Guid.NewGuid(),
		OracleId: Guid.NewGuid(),
		Name: "Commit // Memory",
		Set: "AKH",
		SetName: "Amonkhet",
		CollectorNumber: "211",
		Lang: "en",
		Layout: "split",
		Finishes: ["nonfoil", "foil"],
		FrameEffects: null,
		BorderColor: "black",
		PromoTypes: null,
		CardmarketId: 99002,
		TcgplayerId: null,
		TcgplayerEtchedId: null,
		MultiverseIds: null);

	static readonly ReferenceCard EtchedFoilOnly = new(
		Id: Guid.NewGuid(),
		OracleId: Guid.NewGuid(),
		Name: "Some Etched-Only Reprint",
		Set: "CMM",
		SetName: "Commander Masters",
		CollectorNumber: "999",
		Lang: "en",
		Layout: "normal",
		Finishes: ["etched"],
		FrameEffects: ["legendary"],
		BorderColor: "borderless",
		PromoTypes: null,
		CardmarketId: 721967,
		TcgplayerId: null,
		TcgplayerEtchedId: 503567,
		MultiverseIds: null);

	static List<ReferenceCard> Fixture() => [LightningBoltM11, DelverOfSecretsTransform, CommitMemorySplit, ClueToken, EtchedFoilOnly];

	ReferenceCardCatalog Catalog() => new(Fixture());

	[Fact]
	public void Count_MatchesFixtureSize()
	{
		Catalog().Count.Should().Be(5);
	}

	[Fact]
	public void FindById_Hit_ReturnsCard()
	{
		Catalog().FindById(LightningBoltM11.Id).Should().Be(LightningBoltM11);
	}

	[Fact]
	public void FindById_Miss_ReturnsNull()
	{
		Catalog().FindById(Guid.NewGuid()).Should().BeNull();
	}

	[Fact]
	public void FindByCardmarketId_HitsKnownPrintings()
	{
		var c = Catalog();
		c.FindByCardmarketId(5395).Should().Be(LightningBoltM11);
		c.FindByCardmarketId(99001).Should().Be(DelverOfSecretsTransform);
		c.FindByCardmarketId(721967).Should().Be(EtchedFoilOnly);
	}

	[Fact]
	public void FindByCardmarketId_MissesUnknownAndCardsWithoutId()
	{
		var c = Catalog();
		c.FindByCardmarketId(11111111).Should().BeNull();   // unknown id
		c.FindByCardmarketId(0).Should().BeNull();          // never assigned
	}

	[Fact]
	public void FindBySetAndCollectorNumber_Works()
	{
		var c = Catalog();
		c.FindBySetAndCollectorNumber("M11", "149").Should().Be(LightningBoltM11);
		c.FindBySetAndCollectorNumber("M11", "999").Should().BeNull();
		c.FindBySetAndCollectorNumber("XYZ", "149").Should().BeNull();
	}

	[Fact]
	public void FindBySetAndCollectorNumber_IsCaseInsensitive_NormalizedAtBoundary()
	{
		// Set codes are stored uppercase canonical; the lookup method normalizes input casing
		// once at the entry point so callers don't need to. "m11" and "M11" both resolve.
		var lower = Catalog().FindBySetAndCollectorNumber("m11", "149");
		var upper = Catalog().FindBySetAndCollectorNumber("M11", "149");
		lower.Should().NotBeNull().And.Be(upper);
	}

	[Fact]
	public void GetSets_ReturnsDistinctSetCodes()
	{
		var sets = Catalog().GetSets();
		sets.Should().HaveCount(5);
		sets.Should().Contain(new KeyValuePair<string, string>("M11", "Magic 2011"));
		sets.Should().Contain(new KeyValuePair<string, string>("ISD", "Innistrad"));
		sets.Should().Contain(new KeyValuePair<string, string>("TMH2", "Modern Horizons 2 Tokens"));
		sets.Should().Contain(new KeyValuePair<string, string>("CMM", "Commander Masters"));
	}

	[Fact]
	public void ExpandFrontFaceToFullName_HitsFrontFace_ReturnsFullName()
	{
		Catalog().ExpandFrontFaceToFullName("Delver of Secrets")
			.Should().Be("Delver of Secrets // Insectile Aberration");
	}

	[Fact]
	public void ExpandFrontFaceToFullName_MissesNonDfcName_ReturnsNull()
	{
		Catalog().ExpandFrontFaceToFullName("Lightning Bolt").Should().BeNull();
	}

	[Fact]
	public void ExpandFrontFaceToFullName_MissesAlreadyFullName_ReturnsNull()
	{
		// "Delver of Secrets // Insectile Aberration" is the full name; it's not a *front face*,
		// so a lookup with the full string should miss (caller should pass a front-face name).
		Catalog().ExpandFrontFaceToFullName("Delver of Secrets // Insectile Aberration").Should().BeNull();
	}

	[Fact]
	public void IsTokenName_OnlyTrueForTokenLayout()
	{
		var c = Catalog();
		c.IsTokenName("Clue").Should().BeTrue();
		c.IsTokenName("Lightning Bolt").Should().BeFalse();
	}

	// Split layouts keep the full "A // B" name on export; transform-like layouts strip to the front face (CardNameConverter relies on this).
	[Theory]
	[InlineData("Commit // Memory", "split")]
	[InlineData("Delver of Secrets // Insectile Aberration", "transform")]
	[InlineData("Lightning Bolt", "normal")]
	public void GetLayoutByName_ReturnsScryfallLayout(string cardName, string expectedLayout)
	{
		Catalog().GetLayoutByName(cardName).Should().Be(expectedLayout);
	}

	[Fact]
	public void GetLayoutByName_MissesUnknownName_ReturnsNull()
	{
		Catalog().GetLayoutByName("Not A Real Card Name").Should().BeNull();
	}

	[Fact]
	public async Task LoadGzipAsync_RoundTripsThroughCompressedJson()
	{
		// Serialize fixture → gzip → load via the public entry point.
		var json = JsonSerializer.Serialize(Fixture(), ReferenceCardCatalog.BundleSerializerOptions);
		var bytes = Encoding.UTF8.GetBytes(json);

		using var compressed = new MemoryStream();
		using (var gz = new GZipStream(compressed, CompressionMode.Compress, leaveOpen: true))
		{
			await gz.WriteAsync(bytes);
		}
		compressed.Position = 0;

		var catalog = await ReferenceCardCatalog.LoadGzipAsync(compressed);

		catalog.Count.Should().Be(5);
		catalog.FindByCardmarketId(5395).Should().NotBeNull().And.BeEquivalentTo(LightningBoltM11);
	}
}
