using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MtgCsvHelper.Maps;
public class MoxfieldMap : CsvToCardMap
{
	public CsvConfig ColumnConfig { get; init; } = new(
			Quantity: "Quantity",
			CardName: "CardName",
			Finish: new(
				HeaderName: "Finish",
				Foil: "Foil",
				Normal: "Normal",
				Etched: ""),
			Condition: null,
			SetCode: "Set Code",
			SetName: "Set Name",
			SetNumber: "Set Number"

			);

	public MoxfieldMap() : base(DeckFormat.MOXFIELD)
	{
		

	}
}
