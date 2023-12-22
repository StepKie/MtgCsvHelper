using Microsoft.Extensions.Configuration;

namespace MtgCsvHelper.Services;
class MtgCardCsvHandlerService(IConfiguration config, IMtgApi api) : IMtgCardCsvHandlerService
{
	public MtgCardCsvHandler GetHandler(string id)
	{
		return new MtgCardCsvHandler(api, config, id);
	}
}
