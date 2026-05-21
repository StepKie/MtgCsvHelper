using System.Globalization;
using System.Text;

namespace MtgCsvHelper.Enrichment;

/// <summary>
/// The one validator in the post-parse pipeline. Drops rows whose (Set, CollectorNumber)
/// doesn't resolve, whose Name doesn't match the resolved printing, or whose Foil flag
/// claims an unsupported finish. Named "Validator" rather than "Enricher" because it
/// checks-and-drops; the only mutation it makes is to the issues collection.
/// </summary>
public sealed class CatalogValidator(IReferenceCardCatalog catalog) : PerCardEnricher
{
	protected override bool EnrichOne(ParsedRow row, ICollection<ImportIssue> issues)
	{
		var p = row.Card.Printing;
		// No catalog lookup is possible without (Name, Set, CollectorNumber). Rows missing any
		// of these have either already been warned by SetInfoEnricher (no set info) or are
		// stubs that a later enricher will fill in (Cardmarket); validating now would error
		// on data that isn't actually broken.
		if (string.IsNullOrEmpty(p.Name)) { return true; }
		if (string.IsNullOrEmpty(p.Set) || string.IsNullOrEmpty(p.CollectorNumber)) { return true; }

		var match = catalog.FindBySetAndCollectorNumber(p.Set, p.CollectorNumber);
		if (match is null)
		{
			issues.Add(new ImportIssue(IssueSeverity.Error, row.RowNumber,
				$"No printing at {p.Set} #{p.CollectorNumber} in Scryfall data",
				CardName: p.Name, RawContent: row.RawContent));
			return false;
		}

		if (!NamesMatch(p.Name, match.Name, catalog))
		{
			issues.Add(new ImportIssue(IssueSeverity.Error, row.RowNumber,
				$"Name '{p.Name}' does not match printing at {p.Set} #{p.CollectorNumber} ('{match.Name}')",
				CardName: p.Name, RawContent: row.RawContent));
			return false;
		}

		if (row.Card.Foil is true && !HasFoilFinish(match.Finishes))
		{
			issues.Add(new ImportIssue(IssueSeverity.Error, row.RowNumber,
				$"Printing {p.Set} #{p.CollectorNumber} was not released in foil",
				CardName: p.Name, RawContent: row.RawContent));
			return false;
		}

		return true;
	}

	static bool NamesMatch(string imported, string referenceName, IReferenceCardCatalog catalog)
	{
		if (EqualsNormalized(imported, referenceName)) { return true; }
		// Front-face-only imports (Moxfield's ShortNames=false formats) match the full Scryfall name.
		var expanded = catalog.ExpandFrontFaceToFullName(imported);
		return expanded is not null && EqualsNormalized(expanded, referenceName);
	}

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

	static bool HasFoilFinish(IReadOnlyList<string> finishes) =>
		finishes.Any(f => string.Equals(f, "foil", StringComparison.OrdinalIgnoreCase)
		               || string.Equals(f, "etched", StringComparison.OrdinalIgnoreCase));
}
