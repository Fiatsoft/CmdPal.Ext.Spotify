using CmdPal.Ext.Spotify.Commands;
using CmdPal.Ext.Spotify.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace CmdPal.Ext.Spotify.Helpers;

public class SettingsManager : JsonSettingsManager
{
    private static readonly string _namespace = "spotify";

    private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";

    internal static string SettingsJsonPath()
    {
        var directory = Utilities.BaseSettingsPath("Microsoft.CmdPal");
        Directory.CreateDirectory(directory);

        return Path.Combine(directory, "settings.json");
    }

    private readonly TextSetting _clientId = new(
        Namespaced(nameof(ClientId)),
        Resources.ExtensionSettingClientId,
        Resources.ExtensionSettingClientIdDescription,
        string.Empty
    );

    public string ClientId => _clientId.Value;

    private readonly TextSetting _filterWildcard = new(
        Namespaced(nameof(FilterWildcard)),
        Resources.ExtensionSettingFilterWildcard,
        Resources.ExtensionSettingFilterWildcardDescription,
        "/"
    );

    public string FilterWildcard => _filterWildcard.Value;

    public CommandResult[] ComandResultsChoices = { CommandResult.Hide(), CommandResult.KeepOpen(), CommandResult.GoBack(), CommandResult.GoHome() };
    public static Dictionary<string, CommandResult> ComandResultsChoicesDictionary;
    public static Dictionary<string, ChoiceSetSetting> CommandResults { get; } = new();
    bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
    {
        while (toCheck != null && toCheck != typeof(object))
        {
            var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
            if (generic == cur)
                return true;
            toCheck = toCheck.BaseType;
        }
        return false;
    }
    private static string? TryGetResource(string key)
    {
        try
        {
            var value = Resources.ResourceManager.GetString(key);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch
        {
            return null;
        }
    }
    private static string InsertSpacesInPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;
        return System.Text.RegularExpressions.Regex.Replace(input, @"(?<!^)([A-Z])", " $1");
    }

    private ChoiceSetSetting marketCountrySetting;
    public string MarketCountryCode { get { return marketCountrySetting.Value;  } }

    public SettingsManager()
    {

        FilePath = SettingsJsonPath();

        Settings.Add(_clientId);
        Settings.Add(_filterWildcard);

        try
        {
            ComandResultsChoicesDictionary = ComandResultsChoices.ToDictionary(c => c.Kind.ToString());
            var choices = new List<ChoiceSetSetting.Choice>(ComandResultsChoices.Select(c => new ChoiceSetSetting.Choice(Resources.ResourceManager.GetString($"ExtensionSettingCommandResultAction{c.Kind.ToString()}"), c.Kind.ToString())));

            var baseType = typeof(CmdPal.Ext.Spotify.Commands.PlayerCommand<>);
            var types = baseType.Assembly
                .GetTypes()
                .Where(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    t.Namespace == "CmdPal.Ext.Spotify.Commands" &&
                    IsSubclassOfRawGeneric(baseType, t))
                .ToList();

            foreach (var type in types)
            {
                var commandName = type.Name;
#if DEBUG
                if (commandName != "AddToQueueCommand")
                    continue;
#endif
                string label = TryGetResource($"Name{commandName}") ?? InsertSpacesInPascalCase(commandName);
                CommandResults.Add(commandName, new ChoiceSetSetting(
                    key: commandName,
                    label: string.Format(Resources.ExtensionSettingCommandResultLabel, label),
                    description: string.Format(Resources.ExtensionSettingCommandResultDesc, label),
                    choices: choices
                ));
            }
            foreach (var choiceSetSetting in CommandResults.Values)
                Settings.Add(choiceSetSetting);
        }
        catch (Exception ex)
        {
            Journal.Append($"Could not initialize CommandResult settings: {ex.Message}", label: Journal.Label.Error);
        }

        try
        {
            var countryDict = new Dictionary<string, string>();
            var choices = new List<ChoiceSetSetting.Choice>(Spotify.Helpers.SpotifyMarkets.Native.Select(c => new ChoiceSetSetting.Choice($"{c.Value} ({c.Key})", c.Key)));
            this.marketCountrySetting = new ChoiceSetSetting(
                key: "marketCountrySetting",
                label: Resources.ExtensionSettingMarketLabel,
                description: Resources.ExtensionSettingMarketDesc,
                choices: choices
            );
            var detectedCountryCode = RegionInfo.CurrentRegion?.TwoLetterISORegionName;
            marketCountrySetting.Value = detectedCountryCode ?? "US";
            Settings.Add(marketCountrySetting);
        }
        catch (Exception ex)
        {
            Journal.Append($"Could not initialize Market settings: {ex.Message}", label: Journal.Label.Error);
        }

        LoadSettings();

        Settings.SettingsChanged += (s, a) => SaveSettings();
    }
}
