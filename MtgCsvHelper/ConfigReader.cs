using Microsoft.Extensions.Configuration;
using MtgCsvHelper;
using Serilog;

namespace MtgCsvHelper;

public static class ConfigReader
{
    public static T? GetFromRoot<T>(this IConfiguration config, string section)
    {
        T configObject = config.GetSection(section).Get<T>();
        Log.Debug($"{section} in config file: {configObject}");
        return configObject;
    }

    public static ColumnNames? GetColumnNames(this IConfiguration config, DeckFormat format) => GetFromRoot<ColumnNames>(config, format.ToString());
}
