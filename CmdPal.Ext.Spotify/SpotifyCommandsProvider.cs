using CmdPal.Ext.Spotify.Helpers;
using CmdPal.Ext.Spotify.Pages;
using CmdPal.Ext.Spotify.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using SpotifyAPI.Web.Auth;

namespace CmdPal.Ext.Spotify;

public partial class SpotifyCommandsProvider : CommandProvider
{
    private readonly CommandItem _command;
    private static readonly SettingsManager SettingsManager = new();
    private readonly SpotifyListPage _spotifyExtensionPage = new(SettingsManager);
    internal static EmbedIOAuthServer EmbedIOAuthServer;

    public SpotifyCommandsProvider()
    {
        DisplayName = Resources.ExtensionDisplayName;
        Id = "Spotify";
        Icon = Icons.Spotify;
        Settings = SettingsManager.Settings;

        _command = new CommandItem(_spotifyExtensionPage)
        {
            Title = Resources.ExtensionDisplayName,
            Subtitle = Resources.ExtensionDescription,
            Icon = Icons.Spotify,
            MoreCommands = [new CommandContextItem(Settings.SettingsPage)]
        };
    }

    public override ICommandItem[] TopLevelCommands() => [_command];
}