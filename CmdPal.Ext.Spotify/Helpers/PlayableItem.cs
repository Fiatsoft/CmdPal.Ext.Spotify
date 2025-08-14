using CmdPal.Ext.Spotify.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CmdPal.Ext.Spotify.Helpers
{
    internal class PlayableItem
    {
        internal static IEnumerable<ListItem> ListItems(IList<PlaylistTrack<IPlayableItem>> For, SpotifyClient _spotifyClient, IList<Type> Without = null)
        {
            if (Without == null)
                Without = new List<Type>();
            return For.Where(playListItem => playListItem != null)
                .SelectMany(playListItem =>
                {
                    try
                    {
                        var item = playListItem.Track.Type switch
                        {
                            ItemType.Track => CmdPal.Ext.Spotify.Helpers.Track.ListItems(new[] { (FullTrack)playListItem.Track }, _spotifyClient, Without).First(),
                            ItemType.Episode => CmdPal.Ext.Spotify.Helpers.Episode.ListItems(new[] { (SimpleEpisode)playListItem.Track }, _spotifyClient, Without).First(),
                            ItemType.Chapter => CmdPal.Ext.Spotify.Helpers.Chapter.ListItems(new[] { (FullAudiobookChapter)playListItem.Track }, _spotifyClient, Without).First(),
                            _ => throw new NotSupportedException()
                        };
                        return new[] { item };
                    }
                    catch (Exception ex)
                    {
                        new ToastStatusMessage(new StatusMessage() { Message = Resources.ErrorUnsupportedPlaylistItem, State = MessageState.Info }).Show();
                        Journal.Append($"{Resources.ErrorUnsupportedPlaylistItem}: {ex.Message}: playListItem: {JsonConvert.SerializeObject(playListItem)}");
                        return Array.Empty<ListItem>(); // skip
                    }
                });
        }
    }
}
