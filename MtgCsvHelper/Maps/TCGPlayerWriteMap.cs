namespace MtgCsvHelper.Maps;

/// <summary>
/// Extends <see cref="PhysicalCardMap"/> with a TCGplayer-specific <c>Name</c> column carrying the
/// base name plus the variant suffix (<c>(Borderless)</c>/<c>(Showcase)</c>/<c>(Extended Art)</c>)
/// that TCGplayer's name-matching importer needs to resolve the correct printing; <c>Simple Name</c>
/// (from the base map) stays plain. Write-only — reads use the plain <c>Simple Name</c> via the base map.
/// </summary>
public sealed class TCGPlayerWriteMap : PhysicalCardMap
{
	public TCGPlayerWriteMap(FormatConfig cfg, IReferenceCardCatalog catalog) : base(cfg, catalog)
	{
		// useExistingMap: false — the base map already maps Printing.Name to "Simple Name", so this adds a second, independent column.
		Map(c => c.Printing.Name, useExistingMap: false).Name("Name").Convert(args =>
		{
			var printing = args.Value.Printing;
			var reference = string.IsNullOrEmpty(printing.Set) || string.IsNullOrEmpty(printing.CollectorNumber)
				? null
				: catalog.FindBySetAndCollectorNumber(printing.Set, printing.CollectorNumber);

			return reference is null ? printing.Name : TcgplayerVariantName.Decorate(printing.Name, reference);
		});
	}
}
