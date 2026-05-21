namespace MtgCsvHelper.Enrichment;

/// <summary>
/// Canonicalizes (Set ↔ SetName) against the catalog. When the set code resolves, SetName is
/// rewritten to the Scryfall canonical name even if the CSV supplied something else (Deckbox's
/// "Extras: …" / "Promo Pack: …" curated names round-trip back to canonical that way).
/// When only the name is supplied, Set is filled in from the reverse lookup. The catalog is
/// authoritative; the CSV values are hints. Adds a Warning when the row has no set information
/// at all, or when neither side resolves.
/// </summary>
public sealed class SetInfoEnricher(IReferenceCardCatalog catalog) : PerCardEnricher
{
	protected override bool EnrichOne(ParsedRow row, ICollection<ImportIssue> issues)
	{
		var p = row.Card.Printing;
		// Skip rows that have no Name — there's nothing meaningful to enrich without a card
		// identity, and any "no set info" warning would compound the issue rather than describe
		// it. In practice this covers Cardmarket stubs awaiting CardmarketIdEnricher resolution.
		if (string.IsNullOrEmpty(p.Name)) { return true; }

		bool hadSetCode = p.Set is not null;
		bool hadSetName = p.SetName is not null;

		// Both directions are O(1) — the catalog maintains forward (code → name) and reverse (name → code) indexes.
		// Prefer the catalog's canonical name over whatever the CSV supplied: keeps the in-memory model
		// stable across formats, so a Deckbox import doesn't leave "Extras: Foo" sitting in SetName.
		if (hadSetCode) { p.SetName = catalog.GetSetNameByCode(p.Set!) ?? p.SetName; }
		if (hadSetName) { p.Set ??= catalog.GetSetCodeByName(p.SetName!); }
		// Rewrite Set to its canonical Scryfall form once SetName is known. Catches MTGO aliases
		// (MI → MIR) and any lowercase input so downstream writes emit the code other tools expect.
		if (p.SetName is not null) { p.Set = catalog.GetSetCodeByName(p.SetName) ?? p.Set; }

		if (!hadSetCode && !hadSetName)
		{
			issues.Add(new ImportIssue(IssueSeverity.Warning, row.RowNumber, "No set information found in row", CardName: p.Name));
		}
		else if (hadSetCode && p.SetName is null)
		{
			issues.Add(new ImportIssue(IssueSeverity.Warning, row.RowNumber, $"Set code '{p.Set}' not found in Scryfall data", CardName: p.Name));
		}
		else if (hadSetName && p.Set is null)
		{
			issues.Add(new ImportIssue(IssueSeverity.Warning, row.RowNumber, $"Set name '{p.SetName}' not found in Scryfall data", CardName: p.Name));
		}

		return true;
	}
}
