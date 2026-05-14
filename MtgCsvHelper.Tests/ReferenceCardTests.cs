namespace MtgCsvHelper.Tests;

public class ReferenceCardTests
{
	[Fact]
	public void CreateFromScryfall_AppliesDefaults_ForNullInputs()
	{
		// Null lang/layout/finishes is the empirically observed Scryfall shape for tokens, emblems,
		// and some special printings. The factory has to substitute the canonical defaults; this test
		// pins down each fallback so a future change to the factory can't silently drop one of them.
		var input = new ScryfallCardJson(
			Id: Guid.Empty,
			OracleId: null,
			Name: "Test",
			Set: "TST",
			SetName: "Test Set",
			CollectorNumber: "1",
			Lang: null,
			Layout: null,
			Finishes: null,
			FrameEffects: null,
			BorderColor: null,
			PromoTypes: null,
			CardmarketId: null,
			TcgplayerId: null,
			TcgplayerEtchedId: null,
			MultiverseIds: null);

		var result = ReferenceCard.CreateFromScryfall(input);

		result.Lang.Should().Be("en");
		result.Layout.Should().Be("normal");
		result.Finishes.Should().BeEmpty();
	}

	[Fact]
	public void CreateFromScryfall_EmptyLangOrLayout_AlsoFallsBack()
	{
		// Distinct from the null case: empty-string lang/layout (rare but observed) also has to fall
		// through to the canonical defaults — `??` alone wouldn't catch it.
		var input = new ScryfallCardJson(
			Id: Guid.Empty,
			OracleId: null,
			Name: "Test",
			Set: "TST",
			SetName: "Test Set",
			CollectorNumber: "1",
			Lang: "",
			Layout: "",
			Finishes: ["nonfoil"],
			FrameEffects: null,
			BorderColor: null,
			PromoTypes: null,
			CardmarketId: null,
			TcgplayerId: null,
			TcgplayerEtchedId: null,
			MultiverseIds: null);

		var result = ReferenceCard.CreateFromScryfall(input);

		result.Lang.Should().Be("en");
		result.Layout.Should().Be("normal");
		result.Finishes.Should().BeEquivalentTo(["nonfoil"]);
	}

	[Fact]
	public void CreateFromScryfall_PassesThroughNonDefaultedFields()
	{
		var input = new ScryfallCardJson(
			Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
			OracleId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
			Name: "Lightning Bolt",
			Set: "M11",
			SetName: "Magic 2011",
			CollectorNumber: "149",
			Lang: "de",
			Layout: "split",
			Finishes: ["nonfoil", "foil"],
			FrameEffects: ["showcase"],
			BorderColor: "black",
			PromoTypes: ["promo"],
			CardmarketId: 5395,
			TcgplayerId: 1174,
			TcgplayerEtchedId: 999,
			MultiverseIds: [209, 210]);

		var result = ReferenceCard.CreateFromScryfall(input);

		result.Lang.Should().Be("de");
		result.Layout.Should().Be("split");
		result.Finishes.Should().BeEquivalentTo(["nonfoil", "foil"]);
		result.FrameEffects.Should().BeEquivalentTo(["showcase"]);
		result.BorderColor.Should().Be("black");
		result.CardmarketId.Should().Be(5395);
		result.TcgplayerId.Should().Be(1174);
		result.TcgplayerEtchedId.Should().Be(999);
		result.MultiverseIds.Should().BeEquivalentTo([209, 210]);
	}
}
