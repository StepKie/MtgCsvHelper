namespace MtgCsvHelper.BlazorWebAssembly;

/// <summary>
/// Builds Scryfall card-image URLs from the Scryfall UUID. Uses the public CDN
/// (cards.scryfall.io) which is rate-limit-free and CDN-cached.
/// </summary>
public static class ScryfallImage
{
	const string CdnBase = "https://cards.scryfall.io";

	public static string Url(Guid scryfallId)
	{
		var hex = scryfallId.ToString("D");
		return $"{CdnBase}/normal/front/{hex[0]}/{hex[1]}/{hex}.jpg";
	}

	/// <summary>Canonical Scryfall page for a specific printing.</summary>
	public static string CardPageUrl(string setCode, string collectorNumber) =>
		$"https://scryfall.com/card/{setCode.ToLowerInvariant()}/{collectorNumber}";

	/// <summary>Scryfall set listing page.</summary>
	public static string SetPageUrl(string setCode) =>
		$"https://scryfall.com/sets/{setCode.ToLowerInvariant()}";
}
