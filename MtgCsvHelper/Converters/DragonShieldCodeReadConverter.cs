using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

/// <summary>On read from a Dragon Shield CSV, collapses GK1_*/GK2_* guild-kit codes to the canonical Scryfall code (GK1/GK2); all other codes pass through to UpperCaseConverter.</summary>
internal sealed class DragonShieldCodeReadConverter : UpperCaseConverter
{
	public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{
		if (text is not null
			&& (text.StartsWith("GK1_", StringComparison.OrdinalIgnoreCase) || text.StartsWith("GK2_", StringComparison.OrdinalIgnoreCase)))
		{
			return text[..3].ToUpperInvariant();
		}

		return base.ConvertFromString(text, row, memberMapData);
	}
}
