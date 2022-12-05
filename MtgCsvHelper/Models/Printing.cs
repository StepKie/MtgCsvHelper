namespace MtgCsvHelper.Models;

public record Printing
{
	public required MtgCard Card { get; init; }

	public string Identifier => $"{Set.Code}#{IdInSet}";

	/// <summary> not int since sometimes it might be followed with a letter like "40s" "40p" etc </summary>
	public required string IdInSet { get; init; }

	public required Set Set { get; init; }
}
