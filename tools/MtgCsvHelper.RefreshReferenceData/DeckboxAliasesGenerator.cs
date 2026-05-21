using System.IO.Compression;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using MtgCsvHelper;

namespace MtgCsvHelper.RefreshReferenceData;

// Generates MtgCsvHelper/Resources/deckbox-set-aliases.json by scraping
// https://deckbox.org/editions and diffing each entry's name against the Scryfall
// canonical name in our bundled catalog. Output shape: { "<scryfall_code>": "<deckbox name>" }
// — only sets whose Deckbox name differs from Scryfall's are listed; the writer falls back
// to the Scryfall canonical for everything else.
//
// The bridge is the `esym_<code>` CSS class on each row, which Deckbox keys to the
// Scryfall-equivalent set symbol. For "Extras: …" (token) and "Promo Pack: …" (promo)
// rows the esym points to the *parent* set, so we additionally try the standard Scryfall
// token-/promo-set code prefixes (`t<code>`, `p<code>`).
//
// Usage:  dotnet run --project tools/MtgCsvHelper.RefreshReferenceData -- deckbox-aliases
internal static class DeckboxAliasesGenerator
{
	const string EditionsUrl = "https://deckbox.org/editions";

	public static async Task RunAsync()
	{
		Console.WriteLine($"Fetching {EditionsUrl} …");
		using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
		http.DefaultRequestHeaders.UserAgent.ParseAdd(AppInfo.UserAgent);
		var html = await http.GetStringAsync(EditionsUrl);
		Console.WriteLine($"  {html.Length:N0} bytes downloaded.");

		var entries = ParseEditions(html);
		Console.WriteLine($"  Parsed {entries.Count} Deckbox edition entries.");

		var bundlePath = Path.GetFullPath(Path.Combine(
			AppContext.BaseDirectory, "..", "..", "..", "..", "..",
			"MtgCsvHelper.BlazorWebAssembly", "wwwroot", "data", "cards.min.json.gz"));
		Console.WriteLine($"Loading Scryfall catalog from {bundlePath} …");
		await using var fs = File.OpenRead(bundlePath);
		var catalog = await ReferenceCardCatalog.LoadGzipAsync(fs);
		var sets = catalog.GetSets(); // Set code (UPPER) → Scryfall canonical name
		Console.WriteLine($"  {sets.Count} Scryfall sets loaded.");

		// Two output maps emitted in lockstep:
		//   - SetCodeToDeckboxName  (deckbox-set-aliases.json):  Scryfall code → Deckbox edition name
		//     Used on the WRITE side to emit the Edition column with Deckbox's curated names.
		//   - DeckboxCodeToSetCode  (deckbox-code-aliases.json): Deckbox edition code → Scryfall code
		//     Used on the READ side to translate Deckbox-internal codes like `ex_127` into the
		//     Scryfall codes the catalog can resolve. Only emitted when the codes differ.
		var codeAliases = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		// Build the alias map across three Deckbox row shapes:
		//   1. Canonical regular sets — Deckbox code matches esym (e.g. AFC/afc, M14/m14).
		//      Map esym (upper) → Deckbox name. Skip if Deckbox already matches Scryfall.
		//   2. "Promo Pack: …" rows — Deckbox code like PP_NEO, esym is the parent (neo).
		//      The corresponding Scryfall set is `p<esym>` (PNEO, "Kamigawa: Neon Dynasty Promos").
		//   3. "Extras: …" rows (tokens) — Deckbox code like EX_45, esym is the parent.
		//      The corresponding Scryfall set is `t<esym>` (TM14, "Magic 2014 Tokens").
		// Sub-products that don't fit any of these (OAFC, OS_*, AFR_AMP, …) are skipped — they
		// reuse a parent set's esym but don't correspond to a distinct Scryfall set we'd ever
		// emit.
		// Reverse name index. Scryfall set names are unique in our catalog so a Dictionary suffices.
		var scryfallCodeByName = sets.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.Ordinal);

		var aliases = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		int aliasedByName = 0, aliasedByCode = 0, aliasedPromo = 0, aliasedToken = 0;
		int skippedExactMatch = 0, skippedSubProduct = 0;
		foreach (var e in entries)
		{
			var name = WebUtility.HtmlDecode(e.DeckboxName);

			// 1. Exact-name match — most reliable, catches legacy-code cases (1E↔LEA, AL↔ALL,
			//    PLIST↔PLST) where the names agree but Deckbox uses old codes.
			if (scryfallCodeByName.TryGetValue(name, out var matchedByName))
			{
				if (!string.Equals(e.DeckboxCode, matchedByName, StringComparison.OrdinalIgnoreCase))
				{
					codeAliases[e.DeckboxCode.ToLowerInvariant()] = matchedByName;
					aliasedByName++;
				}
				else { skippedExactMatch++; }
				continue;
			}

			// 2. "Extras: …" (tokens) — esym is the parent set; Scryfall token sets are `t<parent>`.
			if (name.StartsWith("Extras:", StringComparison.Ordinal))
			{
				var tokenCode = "T" + e.EsymCode.ToUpperInvariant();
				if (sets.ContainsKey(tokenCode))
				{
					aliases[tokenCode.ToLowerInvariant()] = name;
					if (!string.Equals(e.DeckboxCode, tokenCode, StringComparison.OrdinalIgnoreCase))
					{
						codeAliases[e.DeckboxCode.ToLowerInvariant()] = tokenCode;
					}
					aliasedToken++;
					continue;
				}
			}

			// 3. "Promo Pack: …" — esym is parent; Scryfall promo sets are `p<parent>`.
			if (name.StartsWith("Promo Pack:", StringComparison.Ordinal))
			{
				var promoCode = "P" + e.EsymCode.ToUpperInvariant();
				if (sets.ContainsKey(promoCode))
				{
					aliases[promoCode.ToLowerInvariant()] = name;
					if (!string.Equals(e.DeckboxCode, promoCode, StringComparison.OrdinalIgnoreCase))
					{
						codeAliases[e.DeckboxCode.ToLowerInvariant()] = promoCode;
					}
					aliasedPromo++;
					continue;
				}
			}

			// 4. Deckbox code matches a Scryfall code → Scryfall set exists but Deckbox renamed it
			//    ("Magic 2014" → "Magic 2014 Core Set", "Forgotten Realms Commander" → "Adventures
			//    in the Forgotten Realms Commander"). Emit a name alias; codes already agree so no
			//    code alias needed.
			var deckboxUpper = e.DeckboxCode.ToUpperInvariant();
			if (sets.TryGetValue(deckboxUpper, out var scryfallNameByCode)
				&& !string.Equals(scryfallNameByCode, name, StringComparison.Ordinal))
			{
				aliases[deckboxUpper.ToLowerInvariant()] = name;
				aliasedByCode++;
				continue;
			}

			// 5. Sub-products (OAFC, AFR_AMP, …) and Deckbox-only editions (Summer of Magic) fall
			//    through. Don't trust esym alone — it's the set *symbol*, which Deckbox reuses for
			//    arbitrary promos and box-topper variants that aren't the parent set.
			skippedSubProduct++;
		}

		// 4. "Love your LGS" is a one-to-many: a single Deckbox edition that aggregates every
		//    Scryfall PLGYY set. Map them all to the same Deckbox name.
		var lgs = entries.FirstOrDefault(e => e.DeckboxName.Equals("Love your LGS", StringComparison.Ordinal));
		int aliasedLgs = 0;
		if (lgs is not null)
		{
			foreach (var (code, _) in sets.Where(kv => kv.Value.StartsWith("Love Your LGS ", StringComparison.Ordinal)))
			{
				aliases[code.ToLowerInvariant()] = lgs.DeckboxName;
				aliasedLgs++;
			}
		}

		Console.WriteLine($"  Exact-name matches (code aliases):     {aliasedByName}");
		Console.WriteLine($"  Code matches w/ Deckbox-renamed sets:  {aliasedByCode}");
		Console.WriteLine($"  Promo Pack aliases:                    {aliasedPromo}");
		Console.WriteLine($"  Extras (token) aliases:                {aliasedToken}");
		Console.WriteLine($"  LGS expansions:                        {aliasedLgs}");
		Console.WriteLine($"  Canonical exact-match (no alias needed): {skippedExactMatch}");
		Console.WriteLine($"  Sub-product rows skipped:                {skippedSubProduct}");
		Console.WriteLine($"  Code aliases (Deckbox → Scryfall):       {codeAliases.Count}");

		var resourcesDir = Path.GetFullPath(Path.Combine(
			AppContext.BaseDirectory, "..", "..", "..", "..", "..",
			"MtgCsvHelper", "Resources"));
		Directory.CreateDirectory(resourcesDir);
		var jsonOpts = new JsonSerializerOptions
		{
			WriteIndented = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		};
		var aliasesPath = Path.Combine(resourcesDir, "deckbox-set-aliases.json");
		await File.WriteAllTextAsync(aliasesPath, JsonSerializer.Serialize(aliases, jsonOpts) + Environment.NewLine);
		Console.WriteLine($"Wrote {aliases.Count} set-name aliases to {aliasesPath}.");

		var codeAliasesPath = Path.Combine(resourcesDir, "deckbox-code-aliases.json");
		await File.WriteAllTextAsync(codeAliasesPath, JsonSerializer.Serialize(codeAliases, jsonOpts) + Environment.NewLine);
		Console.WriteLine($"Wrote {codeAliases.Count} code aliases to {codeAliasesPath}.");
	}

	// One row of the editions table: name as Deckbox displays it, Deckbox's internal set code
	// (parens), and the esym class — a `m14`-style key that Deckbox uses for the set symbol.
	// The esym matches the Scryfall code for regular sets; for Extras/Promo Pack rows it points
	// to the parent set instead.
	internal sealed record DeckboxEdition(string DeckboxName, string DeckboxCode, string EsymCode);

	// Matches the per-row pattern:
	//   <svg class='esym_<code>  C' …>
	//   …
	//   <a href="/editions/<id>-<slug>"><name></a>
	//   <span class='note'>(<code>)</span>
	// The `[\s\S]*?` span between the SVG class and the anchor crosses a few sibling tags but
	// stays within one <tr>. A short timeout guards against catastrophic backtracking if
	// Deckbox ever restructures the markup in a way that defeats the lazy quantifier.
	static readonly Regex RowPattern = new(
		@"esym_(?<esym>[a-z0-9_]+)\s+C'\s+data-title=""[^""]+""[\s\S]*?<a href=""/editions/\d+-[^""]+"">(?<name>[^<]+)</a>\s*<span class='note'>\((?<code>[A-Z0-9_]+)\)</span>",
		RegexOptions.Compiled, TimeSpan.FromSeconds(5));

	internal static List<DeckboxEdition> ParseEditions(string html)
	{
		return [.. RowPattern.Matches(html).Select(m =>
			new DeckboxEdition(m.Groups["name"].Value, m.Groups["code"].Value, m.Groups["esym"].Value))];
	}

}
