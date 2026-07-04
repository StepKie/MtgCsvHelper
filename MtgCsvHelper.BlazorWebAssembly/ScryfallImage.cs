using System.Diagnostics.CodeAnalysis;

namespace MtgCsvHelper.BlazorWebAssembly;

/// <summary>
/// Builds Scryfall card-image URLs from the Scryfall UUID. Uses the public CDN
/// (cards.scryfall.io) which is rate-limit-free and CDN-cached.
/// </summary>
public static class ScryfallImage
{
	const string CdnBase = "https://cards.scryfall.io";

	[SuppressMessage("Design", "CA1055:URI-like return values should not be strings", Justification = "Consumed by MudImage.Src and <img src>, both of which take string.")]
	public static string Url(Guid scryfallId)
	{
		var hex = scryfallId.ToString("D");
		return $"{CdnBase}/normal/front/{hex[0]}/{hex[1]}/{hex}.jpg";
	}

	/// <summary>Canonical Scryfall page for a specific printing.</summary>
	[SuppressMessage("Design", "CA1055:URI-like return values should not be strings", Justification = "Consumed by anchor href, which takes string.")]
	[SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Scryfall URLs use lowercase set codes by convention.")]
	public static string CardPageUrl(string setCode, string collectorNumber) =>
		$"https://scryfall.com/card/{setCode.ToLowerInvariant()}/{collectorNumber}";

	/// <summary>Scryfall set listing page.</summary>
	[SuppressMessage("Design", "CA1055:URI-like return values should not be strings", Justification = "Consumed by anchor href, which takes string.")]
	[SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Scryfall URLs use lowercase set codes by convention.")]
	public static string SetPageUrl(string setCode) =>
		$"https://scryfall.com/sets/{setCode.ToLowerInvariant()}";
}
