The files in the Samples folder should represent a minimal set of cards that showcase all kinds of values for

- language
- condition
- foil/non-foil

as well as some cards that are encoded differently for some platforms.
Examples are:

- Tokens
- Double-sided cards

More representative rows may be added as new conversion challenges are identified.

The sample set is for example:

- One card (double faced)
  - repeated seven times
  - with different languages
  - with different conditions

- currently there is no test for tokens

- some sets have less than 7 conditions (for example Moxfield has 6 - for these we have to map two categories on one target category. This also leads to losing information on round trip conversions (e.g. Dragonshield Excellent -> Moxfield Near Mint -> Dragonshield Near Mint)

The sample is:

Folder Name,Quantity,Trade Quantity,Card Name,Set Code,Set Name,Card Number,Condition,Printing,Language,Price Bought,Date Bought,AVG,LOW,TREND
Test,1,1,Ambitious Farmhand,MID,Innistrad: Midnight Hunt,2,Mint,Normal,English,,,,,
Test,2,1,Ambitious Farmhand,MID,Innistrad: Midnight Hunt,2,NearMint,Foil,German,0.20,2022-11-16,0.20,0.03,0.25
Test,1,1,Ambitious Farmhand,MID,Innistrad: Midnight Hunt,2,Good,Normal,English,,,,,
Test,1,0,Ambitious Farmhand,MID,Innistrad: Midnight Hunt,2,LightPlayed,Normal,English,,,,,
Test,1,0,Ambitious Farmhand,MID,Innistrad: Midnight Hunt,2,Played,Normal,English,,,,,
Test,1,0,Ambitious Farmhand,MID,Innistrad: Midnight Hunt,2,Poor,Normal,English,,,,,