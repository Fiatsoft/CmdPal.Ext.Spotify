using CmdPal.Ext.Spotify.Commands;
using CmdPal.Ext.Spotify.Helpers;
using CmdPal.Ext.Spotify.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Media.Protection.PlayReady;

namespace CmdPal.Ext.Spotify.Pages;

internal sealed partial class ShowPage : ListPage
{
    private readonly SpotifyClient spotifyClient;
    private readonly string? _showId;
    private readonly SimpleEpisode? _simpleEpisode;
    private readonly FullEpisode? _fullEpisode;
    private readonly string? _name;
    private List<SimpleEpisode> items;

    // Base private constructor to avoid code duplication
    private ShowPage(SpotifyClient spotify, string? showId = null,
                     SimpleEpisode? simpleEpisode = null,
                     FullEpisode? fullEpisode = null, string? name = null)
    {
        spotifyClient = spotify ?? throw new ArgumentNullException(nameof(spotify));
        _showId = showId;
        _simpleEpisode = simpleEpisode;
        _fullEpisode = fullEpisode;
        _name = name;

        this.Title = String.IsNullOrEmpty(name) ? Resources.ShowPageTitle : name;
        this.Name = this.Title;
        this.Icon = Icons.Spotify;
    }

    public ShowPage(SpotifyClient spotify, string showId, string? name = null)
        : this(spotify, showId: showId, null, null, name) { }

    public ShowPage(SpotifyClient spotify, SimpleShow show, string? name = null)
        : this(spotify, showId: show?.Id, name: name) { }

    public ShowPage(SpotifyClient spotify, FullShow show, string? name = null)
        : this(spotify, showId: show?.Id, name: name) { }

    public ShowPage(SpotifyClient spotify, SimpleEpisode episode, string? name = null)
        : this(spotify, simpleEpisode: episode, name: name) { }

    public ShowPage(SpotifyClient spotify, FullEpisode episode, string? name = null)
        : this(spotify, fullEpisode: episode, name: name) { }

    public override IListItem[] GetItems()
    {
        try
        {
            var resolvedShowId = _showId;
            if (string.IsNullOrEmpty(resolvedShowId))
            {
                if (_simpleEpisode != null)
                {
                    resolvedShowId = spotifyClient.Episodes.Get(_simpleEpisode.Id).Result.Show.Id;
                    this.Icon = new IconInfo(_simpleEpisode.Images.OrderBy(x => x.Width * x.Height).FirstOrDefault()?.Url);
                }
                else if (_fullEpisode != null)
                {
                    resolvedShowId = _fullEpisode.Show?.Id
                        ?? spotifyClient.Episodes.Get(_fullEpisode.Id).Result.Show.Id;
                    this.Icon = new IconInfo(_fullEpisode.Images.OrderBy(x => x.Width * x.Height).FirstOrDefault()?.Url);
                }
            }

            if (string.IsNullOrEmpty(resolvedShowId))
                throw new ArgumentException("No show information provided.");

            var show = spotifyClient.Shows.Get(resolvedShowId).GetAwaiter().GetResult();

            this.Title = show.Name;
            this.items = spotifyClient.PaginateAll(show.Episodes).Result.ToList();
            return CmdPal.Ext.Spotify.Helpers.Episode.ListItems(this.items, spotifyClient, Without: new List<Type>() { typeof(ShowPage)}).ToArray();
        }
        catch (Exception ex)
        {
            new ToastStatusMessage(new StatusMessage() { Message = Resources.ErrorShowPage, State = MessageState.Info }).Show();
            Journal.Append($"{Resources.ResourceManager.GetString("ErrorShowPage", CultureInfo.InvariantCulture)}: {ex.Message}: {JsonConvert.SerializeObject(this)}", label: Journal.Label.Error);
            return new ListItem[] { new ListItem(new AnonymousCommand(() => { return; }) { Name = "Go Back" , Result= CommandResult.GoBack() }) };

        }
    }
}