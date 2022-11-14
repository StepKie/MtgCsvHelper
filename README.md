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



## How to use

* Prerequisite: [Installed .NET Runtime, Version >= 6.0](https://dotnet.microsoft.com/download/dotnet).
* Run the provided MtgCsvHelper.
	* Usage info can be found running it with the *--help* flag
* Some additional configurability for end user via appsettings.json etc.



----------------

