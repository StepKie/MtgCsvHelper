namespace MtgCsvHelper;

public record Printing
{
    public MtgCard Card { get; set; }

    public string Identifier => $"{Set.Id}#{IdInSet}";

    /// <summary> not int since sometimes it might be followed with a letter like "40s" "40p" etc </summary>
    public string IdInSet { get; set; }

    public Set Set { get; set; }
}