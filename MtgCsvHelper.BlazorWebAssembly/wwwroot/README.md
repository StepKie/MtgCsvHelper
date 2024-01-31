# MtG Csv Helper

The purpose of this tool is to automate the conversion between several collection formats for Magic the Gathering used by popular tools.

## TL;DR

You are welcome to try out the web app [here](https://hottemax.github.io/MtgCsvHelper/).

**ATTENTION:** Since this is a client-side Blazor app, the browser may cache data. and this cache needs to be cleared in order to load the latest version of the app.
The version displayed in the upper right corner should match the last commit id displayed on the [main](https://github.com/Hottemax/MtgCsvHelper/tree/main) branch.

1. Upload input csv file
2. Select input and output format
3. Click "Convert". In case of a larger collection, please allow for some processing time.
4. Download converted output csv file

* Currently supported:
  * Moxfield
  * DragonShield
  * Manabox
  * Topdecked
  * Deckbox
  * Mtggoldfish
  * Cardkingdom (Low info, only 4 columns - Export only)

* Potentially added by demand
  * TcgPlayer
  * ???

## Project Info

* Created this tool for my own use since I like to keep my MtG collection up-to-date by using a card scanner.
* The best ones I found for this purpose are [Manabox](https://www.manabox.app/) and [MtG DragonShield Card Manager](https://mtg.dragonshield.com/)
* You can export data from their site, but there is no easy way to import it to some of the most popular other collection managers such as [Moxfield](https://www.moxfield.com/collection) or [Deckbox](https://deckbox.org)
* The other use case, unrelated to card scanners, is to keep your collection in sync across multiple sites


* The manual conversion process is quite cumbersome, one always has to replace the corresponding Csv Headers, and replace some values that are slightly different. For example
  * Card name: infuriatingly, some sites do not properly encode the full name of double-faced cards, but only front-side.
  * Set information: set name or set code missing, non-standard set names
  * Foil status;: encoded widely different across formats
  *Card condition: some sites use a scale of 6, some of 7 conditions. These need to be mapped. Loss of information is inevitable when mapping to lower resolution, though.
  * Language: (code vs full name)
  * Price: (various formats with various currencies, with or without currency symbol, leading or trailing position)
  * etc
* Some columns which are not common to most tools are not supported

This tool defines configurable mappings addressing the above issues in *appsettings.json*

## How to use

### Browser

* A static webapp is available at [https://hottemax.github.io/MtgCsvHelper/](https://hottemax.github.io/MtgCsvHelper/)
* As opposed to the console version, there is currently no way to access the configuration (appsettings) and customize the app's behavior
* However, it is more user-friendly and should work across a wide range of default scenarios

### Console

* Prerequisite: [Installed .NET Runtime, Version >= 7.0](https://dotnet.microsoft.com/download/dotnet).
* You can download from the Releases tab on the right, or you can build from source yourself
  * For building from source, unzip the source folde, and run *dotnet build* in the root directory of the unzipped folder

* Run the provided MtgCsvHelper.
	* Usage info can be found running it with the *--help* flag
* Some additional configurability for end user via appsettings.json etc.
	*


## Troubleshooting

You can report bugs and issues on Github by creating a [new issue](https://github.com/Hottemax/MtgCsvHelper/issues/new/choose).
**After the release of a new version, the browser data/cache should to be cleared** to force the new version of the site to be loaded, since static webapps are kept in the browser's cache.
The version displayed in the upper right corner should match the last commit id displayed on the [main](https://github.com/Hottemax/MtgCsvHelper/tree/main) branch.

## Current state

* In appsettings.json, there are a lot of predefined mappings for popular sites
  * DRAGONSHIELD
  * MOXFIELD
  * DECKBOX
  * MANABOX
  * TCGPLAYER
  * MTGGOLDFISH
  * CARDKINGDOM
  
* You can add a new configuration for a site that is not yet supported, or even better, create a pull request for it.
* The format should be self-documenting. This is an example configuration for MOXFIELD:

```json
"MOXFIELD": {
  "Quantity": "Count",
  "CardName": {
    "HeaderName": "Name",
    "ShortNames": false
  },
  "SetCode": "Edition",
  "SetName": null,
  "SetNumber": "Collector Number",
  "Finish": {
    "HeaderName": "Foil",
    "Normal": "",
    "Foil": "foil",
    "Etched": "etched"
  },
  "Condition": {
    "HeaderName": "Condition",
    "Mint": "Mint",
    "NearMint": "Near Mint",
    "Excellent": "Near Mint",
    "Good": "Good (Lightly Played)",
    "LightlyPlayed": "Played",
    "Played": "Heavily Played",
    "Poor": "Damaged"
  },
  "Language": {
    "HeaderName": "Language",
    "ShortNames": false
  },
  "PriceBought": {
    "HeaderName": "Purchase Price",
    "Currency": "EUR",
    "CurrencySymbol": "Absent"
  }
}
```

* The left hand side of the mappings should be adapted to match the desired format (csv header row)
* For some columns, there is additional configuration needed (for example, how the _Finish_ is encoded
* For the _Condition_ category, note that different sites use different scales (sometimes 6, sometimes 7 different values, as well as different naming schemes). Hence, there is no canonical mapping
* Some formats (e.g. CARDKINGDOM) should not be used for imports, since they contain very few columns

## TODOs

* Support more formats by popular demand
* Add support for token cards (widely different encodings)
	
Examples:

```
Token: (TODO)

Moxfield:		"1","1","Clue","tmh2","Near Mint","English","","","2022-11-15 13:00:49.057000","14"
DragonShield:	1,Clue Token,Modern Horizons 2 Tokens,TMH2,14,NearMint,German,Normal
Deckbox:		1,0,Clue,Extras: Modern Horizons 2,14,Near Mint,German,,,,,,,,$0.00

Double-sided: (FIXED)

Moxfield:		"1","1","Ambitious Farmhand // Seasoned Cathar","mid","Near Mint","German","","","2022-11-14 16:57:33.500000","2"
DragonShield:	Other,1,0,Ambitious Farmhand,MID,Innistrad: Midnight Hunt,2,NearMint,Normal,German,0.01,2022-01-29,0.08,0.02,0.14
Deckbox:		1,0,Ambitious Farmhand // Seasoned Cathar,Innistrad: Midnight Hunt,2,Near Mint,German,,,,,,,,$0.00
```

Basically, we would want to default to Scryfall's syntax (which would mean using the full double-sided name, and omitting the "Token"
