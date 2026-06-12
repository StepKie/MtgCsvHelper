using System.Globalization;
using System.Text;

namespace MtgCsvHelper.Enrichment;

/// <summary>
/// The one validator in the post-parse pipeline. Resolves each row to a catalog printing — by the
/// Scryfall id when the row carries one (authoritative; survives sites that reshape collector
/// numbers), else by (Set, CollectorNumber). Drops rows that don't resolve, whose Name doesn't match
/// the resolved printing, or whose Finish claims an unsupported finish.
/// Named "Validator" rather than "Enricher" because it mainly
/// checks-and-drops; its only mutations are the issues collection, canonicalizing the
/// name of a short-named or front-face-ambiguous double-faced card to the resolved printing,
/// and backfilling Rarity and the Scryfall Id from the resolved printing.
/// </summary>
public sealed class CatalogValidator(IReferenceCardCatalog catalog) : PerCardEnricher
{
	protected override bool EnrichOne(ref ParsedRow row, ICollection<ImportIssue> issues)
	{
		var p = row.Card.Printing;
		// No catalog lookup is possible without (Name, Set, CollectorNumber). Rows missing any
		// of these have either already been warned by SetInfoEnricher (no set info) or are
		// stubs that a later enricher will fill in (Cardmarket); validating now would error
		// on data that isn't actually broken.
		if (string.IsNullOrEmpty(p.Name)) { return true; }

		// A populated Scryfall id pins the exact printing — trust it over (Set, #), which some sites reshape on export.
		if (p.Id != Guid.Empty && catalog.FindById(p.Id) is { } byId)
		{
			if (FinishUnavailable(row, byId, issues)) { return false; }

			// The id is the identity; adopt its name outright — no mismatch guard like the (set, #) path runs.
			p.Name = byId.Name;
			p.Set = byId.Set;
			p.SetName = byId.SetName;
			p.CollectorNumber = byId.CollectorNumber;
			row = row with { Card = row.Card with { Rarity = byId.Rarity } };

			return true;
		}

		if (string.IsNullOrEmpty(p.Set) || string.IsNullOrEmpty(p.CollectorNumber)) { return true; }

		var match = catalog.FindBySetAndCollectorNumber(p.Set, p.CollectorNumber);
		if (match is null)
		{
			// A stale (set, #) coordinate (e.g. a retired set code): rewrite to the current printing by name so the output stays importable.
			var resolved = catalog.ResolveStalePrinting(p.Name, p.Set);
			if (resolved is null)
			{
				issues.Add(new ImportIssue(IssueSeverity.Error, row.RowNumber,
					$"No printing at {p.Set} #{p.CollectorNumber} in Scryfall data",
					CardName: p.Name, RawContent: row.RawContent));
				return false;
			}

			issues.Add(new ImportIssue(IssueSeverity.Warning, row.RowNumber,
				$"No printing at {p.Set} #{p.CollectorNumber} in Scryfall data; rewritten to {resolved.Set} #{resolved.CollectorNumber}",
				CardName: p.Name, RawContent: row.RawContent));

			p.Id = resolved.Id;
			p.Name = resolved.Name;
			p.Set = resolved.Set;
			p.SetName = resolved.SetName;
			p.CollectorNumber = resolved.CollectorNumber;
			row = row with { Card = row.Card with { Rarity = resolved.Rarity } };

			return true;
		}

		if (!EqualsNormalized(p.Name, match.Name))
		{
			if (!EqualsNormalized(FrontFace(p.Name), FrontFace(match.Name)) && !ExtendsCanonicalName(p.Name, match.Name))
			{
				issues.Add(new ImportIssue(IssueSeverity.Error, row.RowNumber,
					$"Name '{p.Name}' does not match printing at {p.Set} #{p.CollectorNumber} ('{match.Name}')",
					CardName: p.Name, RawContent: row.RawContent));
				return false;
			}
			// (Set, #) already pins the printing; adopt its canonical name for a short, shared-front-face, or decorated import.
			p.Name = match.Name;
		}

		if (FinishUnavailable(row, match, issues)) { return false; }

		p.Id = match.Id;
		row = row with { Card = row.Card with { Rarity = match.Rarity } };

		return true;
	}

	// Front face of a DFC name ("A // B" → "A"); a name without " // " is its own front face.
	static string FrontFace(string name) => name.Split(" // ")[0];

	// Exporter decorations ("Beast Token (4/4)", "Morph Creature") merely extend the canonical name; the name check stays a corruption guard.
	static bool ExtendsCanonicalName(string imported, string canonical) =>
		StripDiacritics(imported).StartsWith(StripDiacritics(canonical) + " ", StringComparison.OrdinalIgnoreCase);

	// Compares with Unicode diacritic stripping (NFD + remove combining marks). TCGPlayer and a
	// few other sites normalize "Lim-Dûl's Vault" → "Lim-Dul's Vault" on export; Scryfall keeps the
	// diacritic. Matching naively would reject every accented card. Done in one pass on both sides.
	static bool EqualsNormalized(string a, string b) =>
		string.Equals(StripDiacritics(a), StripDiacritics(b), StringComparison.OrdinalIgnoreCase);

	static string StripDiacritics(string s)
	{
		var decomposed = s.Normalize(NormalizationForm.FormD);
		var sb = new StringBuilder(decomposed.Length);
		foreach (var c in decomposed)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) { sb.Append(c); }
		}
		return sb.ToString();
	}

	/// <summary>A foil/etched mark on a printing never released in that finish is a site data conflict; records an error and signals the row should drop.</summary>
	static bool FinishUnavailable(ParsedRow row, ReferenceCard printing, ICollection<ImportIssue> issues)
	{
		if (row.Card.Finish is not (CardFinish.Foil or CardFinish.Etched) || HasFoilFinish(printing.Finishes)) { return false; }

		issues.Add(new ImportIssue(IssueSeverity.Error, row.RowNumber,
			$"Printing {printing.Set} #{printing.CollectorNumber} was not released in foil",
			CardName: printing.Name, RawContent: row.RawContent));

		return true;
	}

	static bool HasFoilFinish(IReadOnlyList<string> finishes) =>
		finishes.Any(f => string.Equals(f, "foil", StringComparison.OrdinalIgnoreCase)
		               || string.Equals(f, "etched", StringComparison.OrdinalIgnoreCase));
}
