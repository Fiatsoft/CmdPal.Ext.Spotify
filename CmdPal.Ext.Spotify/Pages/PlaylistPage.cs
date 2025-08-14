using CmdPal.Ext.Spotify.Commands;
using CmdPal.Ext.Spotify.Helpers;
using CmdPal.Ext.Spotify.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;

namespace CmdPal.Ext.Spotify.Pages;

internal sealed partial class PlaylistPage : ListPage
{
    private SpotifyClient spotifyClient;
    private List<FullTrack>? items;
    private string playlistId;
    private string playlistName;

    public PlaylistPage(
        SpotifyClient spotifyClient,
        string playlistId,
        string playlistName
    )
    {
        this.spotifyClient = spotifyClient;
        this.playlistId = playlistId;
        this.playlistName = playlistName;

        Icon = Icons.Spotify;
    }

    public override IListItem[] GetItems()
    {
        try
        {
            var playlist = spotifyClient.Playlists.Get(playlistId).GetAwaiter().GetResult();
            Icon = new IconInfo(playlist.Images.OrderBy(x => x.Width * x.Height).FirstOrDefault()?.Url);
            var allTracks = spotifyClient.PaginateAll(playlist.Tracks).Result;
            return CmdPal.Ext.Spotify.Helpers.PlayableItem.ListItems(allTracks, spotifyClient).ToArray();
        }
        catch (Exception ex)
        {
            new ToastStatusMessage(new StatusMessage() { Message = $"Failed to get playlist items", State = MessageState.Info }).Show();
            Journal.Append($"Failed to get playlist items: {ex.Message}: {JsonConvert.SerializeObject(this)}", label: Journal.Label.Error);
            return new ListItem[0];
        }
    }
}