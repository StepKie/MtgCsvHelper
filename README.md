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

* Currently out-of-the-box supports DragonShield Card manager and Moxfield.
* Further targeted: Deckbox, others on demand (please create an issue if you have specific requests)

## TODOs

* Support more formats by popular demand
* Fix mapping between certain decks/card names
	* The main issues encountered here so far were double-faced cards and token cards
	* these differ in the set name as well the card name across formats
	
Examples:

```
Token: 

Moxfield:       "1","1","Clue","tmh2","Near Mint","English","","","2022-11-15 13:00:49.057000","14"
DragonShield:   1,Clue Token,Modern Horizons 2 Tokens,TMH2,14,NearMint,German,Normal
Deckbox:        1,0,Clue,Extras: Modern Horizons 2,14,Near Mint,German,,,,,,,,$0.00

Double-sided:

Moxfield:       "1","1","Ambitious Farmhand // Seasoned Cathar","mid","Near Mint","German","","","2022-11-14 16:57:33.500000","2"
DragonShield:   Other,1,0,Ambitious Farmhand,MID,Innistrad: Midnight Hunt,2,NearMint,Normal,German,0.01,2022-01-29,0.08,0.02,0.14
Deckbox:        1,0,Ambitious Farmhand // Seasoned Cathar,Innistrad: Midnight Hunt,2,Near Mint,German,,,,,,,,$0.00
```

Basically, we would want to default to Scryfall's syntax (which would mean using the full double-sided name, and omitting the "Token"

## How to use

* Prerequisite: [Installed .NET Runtime, Version >= 6.0](https://dotnet.microsoft.com/download/dotnet).
* You can download from the Releases tab on the right, or you can build from source yourself
  * For building from source, unzip the source folde, and run *dotnet build* in the root directory of the unzipped folder

* Run the provided MtgCsvHelper.
	* Usage info can be found running it with the *--help* flag
* Some additional configurability for end user via appsettings.json etc.



----------------

