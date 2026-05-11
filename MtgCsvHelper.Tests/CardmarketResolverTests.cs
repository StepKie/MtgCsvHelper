using MtgCsvHelper.Services;

namespace MtgCsvHelper.Tests;

[Collection(CatalogCollection.Name)]
public class CardmarketResolverTests(CatalogFixture fixture, ITestOutputHelper output) : ApiBaseTest(fixture, output)
{
	/// <summary>
	/// Spy IMtgApi: never expected to be called in catalog-hit tests; if it is, the spy
	/// records the invocation so the test can assert on it.
	/// </summary>
	sealed class SpyMtgApi : IMtgApi
	{
		public List<int> CallsReceived { get; } = [];
		public IReadOnlyDictionary<int, ReferenceCard> NextResponse { get; set; } = new Dictionary<int, ReferenceCard>();

		public Task<IReadOnlyDictionary<int, ReferenceCard>> GetCardsByCardmarketIdsAsync(IEnumerable<int> cardmarketIds, CancellationToken ct = default)
		{
			CallsReceived.AddRange(cardmarketIds);

			return Task.FromResult(NextResponse);
		}
	}

	[Fact]
	public async Task Resolve_IdInCatalog_ReturnsCatalogEntry_WithoutCallingApi()
	{
		// 266380 (Putrid Leech, Conflux) is present in the cardmarket sample CSV and in the bundled catalog.
		var spy = new SpyMtgApi();
		var resolver = new CardmarketResolver(_catalog, spy);

		var resolved = await resolver.ResolveAsync([266380]);

		resolved.Should().ContainKey(266380);
		resolved[266380].Name.Should().Be("Putrid Leech");
		spy.CallsReceived.Should().BeEmpty("catalog hits must not trigger network egress");
	}

	[Fact]
	public async Task Resolve_IdNotInCatalog_FallsBackToApi_WithExactMissingId()
	{
		// 99999999 is far above any real cardmarket_id; guaranteed catalog miss.
		const int missingId = 99999999;
		var cannedCard = new ReferenceCard(
			Id: Guid.NewGuid(),
			OracleId: null,
			Name: "Canned Card",
			Set: "TST",
			SetName: "Test Set",
			CollectorNumber: "1",
			Lang: "en",
			Layout: "normal",
			Finishes: ["nonfoil"],
			FrameEffects: null,
			BorderColor: null,
			PromoTypes: null,
			CardmarketId: missingId,
			TcgplayerId: null,
			TcgplayerEtchedId: null,
			MultiverseIds: null);
		var spy = new SpyMtgApi
		{
			NextResponse = new Dictionary<int, ReferenceCard> { [missingId] = cannedCard }
		};
		var resolver = new CardmarketResolver(_catalog, spy);

		var resolved = await resolver.ResolveAsync([missingId]);

		spy.CallsReceived.Should().BeEquivalentTo([missingId], "the missing id must reach the API exactly once");
		resolved.Should().ContainKey(missingId);
		resolved[missingId].Name.Should().Be("Canned Card");
	}

	[Fact]
	public async Task Resolve_MixedHitsAndMisses_OnlyMissesReachTheApi()
	{
		const int hitId = 266380;             // catalog hit
		const int missId = 99999999;          // catalog miss
		var spy = new SpyMtgApi
		{
			NextResponse = new Dictionary<int, ReferenceCard>() // miss stays unresolved
		};
		var resolver = new CardmarketResolver(_catalog, spy);

		var resolved = await resolver.ResolveAsync([hitId, missId]);

		spy.CallsReceived.Should().BeEquivalentTo([missId], "the catalog hit must not be passed to the API");
		resolved.Should().HaveCount(1);
		resolved.Should().ContainKey(hitId);
		resolved.Should().NotContainKey(missId);
	}
}
