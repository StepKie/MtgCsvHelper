# MtgCsvHelper

Tool for converting Magic: The Gathering card collection CSV files between formats (Moxfield, DragonShield, Manabox, Topdecked, Deckbox, MtgGoldfish, CardKingdom).

## Project Structure

- `MtgCsvHelper/` - Core library (models, services, CSV mapping logic)
- `MtgCsvHelper.Console/` - CLI application
- `MtgCsvHelper.BlazorWebAssembly/` - Blazor WASM web app (deployed to GitHub Pages)
- `MtgCsvHelper.Tests/` - xUnit test project

## Tech Stack

- .NET 9.0, C#
- Blazor WebAssembly for the web frontend
- xUnit + AwesomeAssertions for tests
- CsvHelper for CSV parsing
- Serilog for logging
- Format mappings configured in `MtgCsvHelper/appsettings.json`

## Commands

```bash
# Build
dotnet build MtgCsvHelper.slnx

# Run tests
dotnet test MtgCsvHelper.slnx

# Run console app
dotnet run --project MtgCsvHelper.Console
```

## Conventions

- File-scoped namespaces (`csharp_style_namespace_declarations = file_scoped`)
- 4 spaces indentation for C#, 2 spaces for XML/csproj
- Allman brace style (opening brace on new line)
- PascalCase for types and public members, `_camelCase` for private fields
- Interfaces prefixed with `I`
- Use pattern matching and modern C# features (switch expressions, null propagation, etc.)
- Follow .editorconfig rules (comprehensive analyzer configuration included)
- Tests use xUnit with AwesomeAssertions
