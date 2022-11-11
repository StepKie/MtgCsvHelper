namespace MtgCsvHelper.Models;

public record Set
{
	/// <summary> Short identifier, e.g. AFR, STX, C21 etc. </summary>
	public string Code { get; set; }
	public string FullName { get; set; }

	public DateTime ReleaseDate { get; set; }
}