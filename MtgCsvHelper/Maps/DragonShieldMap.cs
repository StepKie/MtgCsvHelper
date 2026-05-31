using MtgCsvHelper.Converters;

namespace MtgCsvHelper.Maps;

/// <summary>PhysicalCardMap with a Dragon Shield Set-code converter that resolves its proprietary Ravnica Guild Kit codes (GK1_*, GK2_*) to canonical Scryfall codes.</summary>
public sealed class DragonShieldMap : PhysicalCardMap
{
	public DragonShieldMap(FormatConfig cfg, IReferenceCardCatalog catalog)
		: base(cfg, catalog, new DragonShieldCodeReadConverter())
	{
	}
}
