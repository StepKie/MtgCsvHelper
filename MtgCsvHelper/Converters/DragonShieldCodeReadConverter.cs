using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

/// <summary>
/// On read from a Dragon Shield CSV, collapses its Ravnica Guild Kit set codes (GK1_DIMIR,
/// GK2_AZORIU, …) to the canonical Scryfall code (GK1/GK2); the guild suffix is cosmetic and
/// collector numbers are shared across the kit. All other codes pass through to UpperCaseConverter.
/// </summary>
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
