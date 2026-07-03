using System.Text.Json.Serialization;

namespace MtgCsvHelper;

/// <summary>
/// Minimal mirror of the Scryfall JSON shape for a single printing — only the fields we keep
/// in <see cref="ReferenceCard"/>. Used as the deserialization target for both the bulk-data
/// pipeline (bundle generator) and the per-card endpoint (<c>CachedMtgApi</c>), so the field
/// list lives in exactly one place. Nullable where Scryfall may omit the property (<c>oracle_id</c>
/// on tokens/emblems, the various external-id fields). Internal because its shape follows
/// whatever Scryfall ships — consumers should work with <see cref="ReferenceCard"/> instead.
/// </summary>
internal sealed record ScryfallCardJson(
	Guid Id,
	[property: JsonPropertyName("oracle_id")] Guid? OracleId,
	string Name,
	string Set,
	[property: JsonPropertyName("set_name")] string SetName,
	[property: JsonPropertyName("collector_number")] string CollectorNumber,
	string? Lang,
	string? Layout,
	List<string>? Finishes,
	[property: JsonPropertyName("frame_effects")] List<string>? FrameEffects,
	[property: JsonPropertyName("border_color")] string? BorderColor,
	[property: JsonPropertyName("promo_types")] List<string>? PromoTypes,
	[property: JsonPropertyName("cardmarket_id")] int? CardmarketId,
	[property: JsonPropertyName("tcgplayer_id")] int? TcgplayerId,
	[property: JsonPropertyName("tcgplayer_etched_id")] int? TcgplayerEtchedId,
	[property: JsonPropertyName("multiverse_ids")] List<int>? MultiverseIds,
	string? Rarity = null);
