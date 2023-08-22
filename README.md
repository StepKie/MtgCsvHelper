# MtG Csv Helper

The purpose of this command line tool is to automate the conversion between several collection formats used by popular tools.
Currently supports DragonShield Card manager and Moxfield. More formats can be added in appsettings.json


## Project Info

* Created this tool for my own use since I like to keep my MtG collection up-to-date by using a card scanner.
* The best one I found for this purpose is [MtG DragonShield Card Manager](https://mtg.dragonshield.com/)
* You can export data from their site, but there is no easy way to import it to some of the most popular other collection managers such as [Moxfield](https://www.moxfield.com/collection) or [Deckbox](https://deckbox.org)
* To do this, one always has to replace the corresponding Csv Headers, and replace some values that are slightly different (i.e. Condition, Printing enumerations, etc.)
* This tool automates this process through a configurable mappings in *appsettings.json*

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
* The format should be self-documenting. This is an example configuration for DRAGONSHIELD:

```
"DRAGONSHIELD": {
	"Quantity": "Quantity",			// 3
	"CardName": {					// Ambitious Farmhand 
		"HeaderName": "Card Name",
		"ShortNames": true
	},
	"SetCode": "Set Code",			// MID
	"SetName": "Set Name",			// Innistrad: Midnight Hunt
	"SetNumber": "Card Number",		// 2
	"Finish": {						// Foil
		"HeaderName": "Printing",
		"Normal": "Normal",
		"Foil": "Foil",
		"Etched": null
	},
	"Condition": {				// Excellent
	"HeaderName": "Condition",
	"Mint": "Mint",
	"NearMint": "NearMint",
	"Excellent": "Excellent",
	"Good": "Good",
	"LightlyPlayed": "LightPlayed",
	"Played": "Played",
	"Poor": "Poor"
	}
	}
```

* The left hand side of the mappings should be adapted to match the desired format (csv header row)
* For some columns, there is additional configuration needed (for example, how the _Finish_ is encoded
* For the _Condition_ category, note that different sites use different scales (sometimes 6, sometimes 7 different values, as well as different naming schemes). Hence, there is no canonical mapping
* Some formats (e.g. CARDKINGDOM) should not be used for imports, since they contain very few columns

* The most apparent challenge will be the handling of double-faced cards, which sites encode differently (see details below)
* If there is some way to create a lookup dictionary for that, I am all ears





## TODOs

* Support more formats by popular demand
* Fix mapping between certain decks/card names
	* The main issues encountered here so far were double-faced cards and token cards
	* https://scryfall.com/search?as=grid&order=name&q=is%3Adoublesided
	* these differ in the set name as well the card name across formats
	
Examples:

```
Token: 

Moxfield:		"1","1","Clue","tmh2","Near Mint","English","","","2022-11-15 13:00:49.057000","14"
DragonShield:	1,Clue Token,Modern Horizons 2 Tokens,TMH2,14,NearMint,German,Normal
Deckbox:		1,0,Clue,Extras: Modern Horizons 2,14,Near Mint,German,,,,,,,,$0.00

Double-sided:

Moxfield:		"1","1","Ambitious Farmhand // Seasoned Cathar","mid","Near Mint","German","","","2022-11-14 16:57:33.500000","2"
DragonShield:	Other,1,0,Ambitious Farmhand,MID,Innistrad: Midnight Hunt,2,NearMint,Normal,German,0.01,2022-01-29,0.08,0.02,0.14
Deckbox:		1,0,Ambitious Farmhand // Seasoned Cathar,Innistrad: Midnight Hunt,2,Near Mint,German,,,,,,,,$0.00
```

Basically, we would want to default to Scryfall's syntax (which would mean using the full double-sided name, and omitting the "Token"

## How to use

* Prerequisite: [Installed .NET Runtime, Version >= 7.0](https://dotnet.microsoft.com/download/dotnet).
* You can download from the Releases tab on the right, or you can build from source yourself
  * For building from source, unzip the source folde, and run *dotnet build* in the root directory of the unzipped folder

* Run the provided MtgCsvHelper.
	* Usage info can be found running it with the *--help* flag
* Some additional configurability for end user via appsettings.json etc.



----------------

