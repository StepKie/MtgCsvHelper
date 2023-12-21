using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MtgCsvHelper.Services;
class MtgCardCsvHanderService(IConfiguration config, IMtgApi api) : IMtgCardCsvHandlerService
{
	public DeckFormat GetFormat(string id)
	{
		return new DeckFormat(config, id);
	}

	public MtgCardCsvHandler GetHandler(string id)
	{
		return new MtgCardCsvHandler(api, GetFormat(id));
	}
}
