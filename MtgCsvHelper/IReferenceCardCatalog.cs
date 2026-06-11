namespace MtgCsvHelper;

/// <summary>
/// Single source of truth for canonical Scryfall printing data, populated from a build-time
/// bundle. All sync — the catalog is fully loaded once at startup, lookups are dictionary reads.
/// </summary>
/// <remarks>
/// Async network fallback (for cards too new to be in the bundle) is layered on top in a separate
/// implementation; this interface stays sync to make the common path obviously fast.
/// </remarks>
public interface IReferenceCardCatalog
{
	int Count { get; }

	ReferenceCard? FindById(Guid scryfallId);
	ReferenceCard? FindByCardmarketId(int cardmarketId);
	ReferenceCard? FindBySetAndCollectorNumber(string setCode, string collectorNumber);

	/// <summary>
	/// Resolves a card whose (set, collector#) coordinate no longer exists, returning the printing
	/// to rewrite it to so the output stays importable. Prefers the successor set a retired code now
	/// aliases to (e.g. Mystery Booster MB1 → The List), falling back to any printing of the name.
	/// Null if the name isn't in the catalog at all.
	/// </summary>
	ReferenceCard? ResolveStalePrinting(string name, string staleSetCode);

	/// <summary>
	/// Distinct sets present in the catalog. Keys are uppercase set codes (e.g. "ISD"),
	/// values are mixed-case set names as Scryfall delivers them (e.g. "Innistrad").
	/// </summary>
	IReadOnlyDictionary<string, string> GetSets();

	/// <summary>
	/// Returns the set name (mixed case, as Scryfall delivers it) for a given set code.
	/// Lookup is case-insensitive. Null if not present. O(1).
	/// </summary>
	string? GetSetNameByCode(string setCode);

	/// <summary>
	/// Returns the set code (uppercase) for a given set name. Lookup is case-insensitive.
	/// Null if no set in the catalog has this name. O(1).
	/// </summary>
	string? GetSetCodeByName(string setName);

	/// <summary>
	/// Given a front-face-only name (e.g. "Delver of Secrets"), returns the full double-faced
	/// name (e.g. "Delver of Secrets // Insectile Aberration"), or null if no double-faced
	/// printing has that front face. Used to expand short names from sites that export only
	/// the front face of double-faced cards.
	/// </summary>
	string? ExpandFrontFaceToFullName(string frontFaceName);

	/// <summary> True if at least one printing of this exact name is a token-flavored layout. </summary>
	bool IsTokenName(string name);

	/// <summary> Scryfall layout for this exact name (e.g. "split", "transform", "normal"). Null if unknown. </summary>
	string? GetLayoutByName(string name);
}
