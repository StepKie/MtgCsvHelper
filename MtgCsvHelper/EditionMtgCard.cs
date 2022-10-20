public record EditionMtgCard
{
    public MtgCard Card { get; set; }

    public string Identifier => $"{Extension.Id}#{IdInSet}";

    public int IdInSet { get; set; }

    public Extension Extension { get; set; }
}