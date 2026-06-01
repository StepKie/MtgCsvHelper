namespace MtgCsvHelper.Tests;

public class TcgplayerVariantNameTests
{
	static ReferenceCard Ref(string? borderColor = null, IReadOnlyList<string>? frameEffects = null) =>
		new(Id: Guid.NewGuid(), OracleId: null, Name: "Orcish Bowmasters", Set: "LTR", SetName: "…",
			CollectorNumber: "433", Lang: "en", Layout: "normal", Finishes: [], FrameEffects: frameEffects,
			BorderColor: borderColor, PromoTypes: null, CardmarketId: null, TcgplayerId: null,
			TcgplayerEtchedId: null, MultiverseIds: null);

	[Fact]
	public void Borderless_BorderColor_MapsToBorderlessSuffix() =>
		TcgplayerVariantName.SuffixFor(Ref(borderColor: "borderless")).Should().Be("(Borderless)");

	[Theory]
	[InlineData("showcase", "(Showcase)")]
	[InlineData("extendedart", "(Extended Art)")]
	public void FrameEffect_MapsToSuffix(string frameEffect, string expected) =>
		TcgplayerVariantName.SuffixFor(Ref(frameEffects: [frameEffect])).Should().Be(expected);

	[Theory]
	[InlineData("legendary")]
	[InlineData("inverted")]
	[InlineData("enchantment")]
	public void NonVariantFrameEffects_ProduceNoSuffix(string frameEffect) =>
		TcgplayerVariantName.SuffixFor(Ref(frameEffects: [frameEffect])).Should().BeNull();

	[Fact]
	public void NormalPrinting_HasNoSuffix() =>
		TcgplayerVariantName.SuffixFor(Ref(borderColor: "black")).Should().BeNull();

	[Fact]
	public void Borderless_TakesPrecedence_OverFrameEffects() =>
		TcgplayerVariantName.SuffixFor(Ref(borderColor: "borderless", frameEffects: ["showcase"])).Should().Be("(Borderless)");

	[Fact]
	public void Decorate_AppendsSuffix_WhenVariant() =>
		TcgplayerVariantName.Decorate("Orcish Bowmasters", Ref(borderColor: "borderless"))
			.Should().Be("Orcish Bowmasters (Borderless)");

	[Fact]
	public void Decorate_LeavesNamePlain_WhenNormal() =>
		TcgplayerVariantName.Decorate("Orcish Bowmasters", Ref(borderColor: "black"))
			.Should().Be("Orcish Bowmasters");
}
