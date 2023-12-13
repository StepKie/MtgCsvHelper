namespace MtgCsvHelper.Services;

public interface IMtgApi
{
	IEnumerable<Set> GetSets();
}
