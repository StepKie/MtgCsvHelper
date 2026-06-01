using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Models;

// Property order follows PhysicalCardMap's CSV index order (Folder, Count, TradeQuantity,
// Printing (Name/Set/SetName/CollectorNumber), Condition, Foil, Language, PriceBought,
// DateBought). TradeListed has no CSV column today and sits at the end.
public record PhysicalMtgCard
{
	// TODO: Folder and TradeQuantity are importer-presentation metadata, not card identity.
	// Revisit as a per-format sidecar when a second importer asks for similar opt-in fields.
	public string? Folder { get; init; }

	public int Count { get; init; }

	public int TradeQuantity { get; init; }

	public required Card Printing { get; init; }

	public CardCondition Condition { get; init; } = CardCondition.Unknown;

	public bool? Foil { get; init; }

	// Encodes 2-letter ISO language code (queryable via CultureInfo)
	public string? Language { get; init; }

	public Money? PriceBought { get; init; }

	public DateTime? DateBought { get; init; }

	public bool? TradeListed { get; init; }
}
