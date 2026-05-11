namespace MtgCsvHelper;

/// <summary>
/// Stripped Scryfall Card record, the canonical printing reference shipped as
/// a build-time bundle (~7 MB gzipped, ~80k printings). Fields chosen to support:
/// <list type="bullet">
/// <item>identity resolution by Scryfall id, cardmarket_id, tcgplayer_id, set+collector_number, multiverse_id</item>
/// <item>inconsistency detection (foil claimed but printing has no foil finish; set+collector_number out of range; etc.)</item>
/// <item>derived attribute computation (e.g. "(Borderless)" suffix from <see cref="BorderColor"/>/<see cref="FrameEffects"/>)</item>
/// <item>double-faced + token detection from <see cref="Layout"/></item>
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
	IReadOnlyList<int>? MultiverseIds)
{
	/// <summary>
	/// Builds a <see cref="ReferenceCard"/> from the canonical <see cref="ScryfallCardJson"/> wire
	/// shape, applying the defaults in one place: <c>Lang</c> falls back to <c>"en"</c>, <c>Layout</c>
	/// to <c>"normal"</c>, <c>Finishes</c> to an empty list. Both the bundle generator and the runtime
	/// network fallback go through this factory, so the field list and defaulting rules can't
	/// silently drift between them.
	/// </summary>
	public static ReferenceCard CreateFromScryfall(ScryfallCardJson c) => new(
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
		MultiverseIds: c.MultiverseIds);
}
