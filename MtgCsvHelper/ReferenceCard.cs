using MtgCsvHelper.Models;

namespace MtgCsvHelper;

/// <summary>
/// Stripped Scryfall Card record, the canonical printing reference shipped as
/// a build-time bundle (~11 MB gzipped, ~115k printings). Fields chosen to support:
/// <list type="bullet">
/// <item>identity resolution by Scryfall id, cardmarket_id, tcgplayer_id, set+collector_number, multiverse_id</item>
/// <item>inconsistency detection (foil claimed but printing has no foil finish; set+collector_number out of range; etc.)</item>
/// <item>derived attribute computation (e.g. "(Borderless)" suffix from <see cref="BorderColor"/>/<see cref="FrameEffects"/>)</item>
/// <item>double-faced + token detection from <see cref="Layout"/></item>
/// <item>collection statistics (per-<see cref="Rarity"/> breakdown)</item>
/// </list>
/// Always English (the Scryfall <c>default_cards</c> bulk file is English-only). Non-English imports
/// fall back to a live Scryfall lookup at runtime.
/// </summary>
public sealed record ReferenceCard(
	Guid Id,
	Guid? OracleId,
	string Name,
	string Set,
	string SetName,
	string CollectorNumber,
	string Lang,
	string Layout,
	IReadOnlyList<string> Finishes,
	IReadOnlyList<string>? FrameEffects,
	string? BorderColor,
	IReadOnlyList<string>? PromoTypes,
	int? CardmarketId,
	int? TcgplayerId,
	int? TcgplayerEtchedId,
	IReadOnlyList<int>? MultiverseIds,
	// MTGO uses 2-letter codes for older sets (MI, VI, TE...) that don't match Scryfall's
	// 3-letter codes (mir, vis, tmp...). Populated from the /sets endpoint at bundle build time;
	// null for sets MTGO doesn't carry. Catalog uses it as a fallback set-code lookup key.
	string? MtgoCode = null,
	// Unknown in bundles generated before the field existed.
	CardRarity Rarity = CardRarity.Unknown)
{
	// Normalized at construction: Set and MtgoCode are uppercased for any primary-constructor
	// invocation (factory, JSON deserialization, direct ctor in tests). The catalog's lookup
	// invariants depend on uppercase storage. Note: `with { Set = "mir" }` would bypass the
	// initializer; no such callers exist today, but a future one would need to uppercase
	// manually or this can be upgraded to a normalizing init setter.
	public string Set { get; init; } = Set.ToUpperInvariant();
	public string? MtgoCode { get; init; } = MtgoCode?.ToUpperInvariant();

	/// <summary> Single canonical factory used by the bundle generator and the runtime network path. </summary>
	internal static ReferenceCard CreateFromScryfall(ScryfallCardJson c, string? mtgoCode = null) => new(
		Id: c.Id,
		OracleId: c.OracleId,
		Name: c.Name,
		Set: c.Set,
		SetName: c.SetName,
		CollectorNumber: c.CollectorNumber,
		Lang: string.IsNullOrEmpty(c.Lang) ? "en" : c.Lang,
		Layout: string.IsNullOrEmpty(c.Layout) ? "normal" : c.Layout,
		Finishes: c.Finishes ?? [],
		FrameEffects: c.FrameEffects,
		BorderColor: c.BorderColor,
		PromoTypes: c.PromoTypes,
		CardmarketId: c.CardmarketId,
		TcgplayerId: c.TcgplayerId,
		TcgplayerEtchedId: c.TcgplayerEtchedId,
		MultiverseIds: c.MultiverseIds,
		MtgoCode: mtgoCode,
		// Boundary conversion: an unrecognized future Scryfall rarity degrades to Unknown instead of failing the import.
		Rarity: Enum.TryParse<CardRarity>(c.Rarity, ignoreCase: true, out var rarity) ? rarity : CardRarity.Unknown);
}
