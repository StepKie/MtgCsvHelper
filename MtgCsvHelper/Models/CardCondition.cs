namespace MtgCsvHelper.Models;

/// <summary> Card condition grade, ordered best-to-worst after <see cref="Unknown"/>. </summary>
public enum CardCondition
{
	Unknown,
	Mint,
	NearMint,
	Excellent,
	Good,
	LightlyPlayed,
	Played,
	Poor,
}
