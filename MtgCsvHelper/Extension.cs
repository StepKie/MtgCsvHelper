namespace MtgCsvHelper;

public record Set
{
    public string Id { get; set; }
    public string FullName { get; set; }

    public DateTime ReleaseDate { get; set; }
}