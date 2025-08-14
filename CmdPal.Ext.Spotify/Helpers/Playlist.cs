using CmdPal.Ext.Spotify.Commands;
using CmdPal.Ext.Spotify.Pages;
using CmdPal.Ext.Spotify.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CmdPal.Ext.Spotify.Helpers
{
    internal class Playlist
    {
        internal static IEnumerable<ListItem> ListItems(IList<FullPlaylist> For, SpotifyClient _spotifyClient, IList<Type> Without = null)
        {
            if (Without == null)
                Without = new List<Type>();
            return For.Where(episode => episode != null)
                .Select(playlist =>
                {
                    var moreCommands = new List<CommandContextItem>();
                    //if (!Without.Contains(typeof(AddToQueueCommand)))
                    //    moreCommands.Add(new CommandContextItem(new AddToQueueCommand(_spotifyClient, new PlayerAddToQueueRequest(playlist.Id))));
                    if (!Without.Contains(typeof(PlaylistPage)))
                        moreCommands.Add(new CommandContextItem(new PlaylistPage(_spotifyClient, playlist.Id, playlist.Name)
                        {
                            Name = Resources.ContextMenuResultGoToPlaylistTitle, Icon = new IconInfo(playlist.Images.OrderBy(x => x.Width * x.Height).FirstOrDefault()?.Url)
                        }));
                    return new ListItem(new ResumePlaybackCommand(_spotifyClient, playlist.Uri))
                    {
                        Title = playlist.Name,
                        Subtitle = $"{playlist.Tracks.Total} items", //Resources.ResultPlaylistSubTitle,
                        Icon = new IconInfo(playlist.Images.OrderBy(x => x.Width * x.Height).FirstOrDefault()?.Url),
                        MoreCommands = moreCommands.ToArray()
                    };
                }).ToList();
        }
    }
}