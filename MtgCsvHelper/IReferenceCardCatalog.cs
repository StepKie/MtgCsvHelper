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

	/// <summary> Distinct sets present in the catalog. Maps set code to set name. </summary>
	IReadOnlyDictionary<string, string> GetSets();

	/// <summary>
	/// Reverse-lookup: returns the set code (uppercase) for a given set name, case-insensitive.
	/// Null if no set in the catalog has this name. O(1).
	/// </summary>
	string? GetSetCodeByName(string setName);

	/// <summary> True if at least one printing of this exact name is a double-faced layout. </summary>
	bool IsDoubleFacedName(string name);

	/// <summary>
	/// Given a front-face-only name (e.g. "Delver of Secrets"), returns the full double-faced
	/// name (e.g. "Delver of Secrets // Insectile Aberration"), or null if no double-faced
	/// printing has that front face. Used to expand short names from sites that export only
	/// the front face of double-faced cards.
	/// </summary>
	string? ExpandFrontFaceToFullName(string frontFaceName);

	/// <summary> True if at least one printing of this exact name is a token-flavored layout. </summary>
	bool IsTokenName(string name);
}
