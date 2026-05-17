namespace MtgCsvHelper.Enrichment;

/// <summary>
/// Backfills the missing side of (Set ↔ SetName) from the catalog, rewrites Set to canonical
/// Scryfall casing (catches lowercase input and MTGO 2-letter aliases). Adds a Warning when
/// the row has no set information at all, or when the supplied code/name doesn't resolve.
/// </summary>
public sealed class SetInfoEnricher(IReferenceCardCatalog catalog) : PerCardEnricher
{
	protected override bool EnrichOne(ParsedRow row, ICollection<ImportIssue> issues)
	{
		var p = row.Card.Printing;
		// Cardmarket stubs (Name="" with only CardMarketId set) reach here unresolved —
		// CardmarketIdEnricher runs later in the pipeline. Skip them so we don't warn on
		// the still-missing Set; the resolver will fill in Name + Set together.
		if (string.IsNullOrEmpty(p.Name)) { return true; }

		bool hadSetCode = p.Set is not null;
		bool hadSetName = p.SetName is not null;

		// Both directions are O(1) — the catalog maintains forward (code → name) and reverse (name → code) indexes.
		if (hadSetCode) { p.SetName ??= catalog.GetSetNameByCode(p.Set!); }
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
