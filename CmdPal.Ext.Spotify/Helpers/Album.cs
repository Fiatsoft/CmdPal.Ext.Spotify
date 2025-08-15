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
    internal class Album
    {
        internal static IEnumerable<ListItem> ListItems(IList<SimpleAlbum> For, SpotifyClient _spotifyClient, IList<Type> Without = null)
        {
            if (Without == null)
                Without = new List<Type>();
            return For.Where(album => album != null).Select(album =>
            {
                var moreCommands = new List<CommandContextItem>();
                //if (!Without.Contains(typeof(AddToQueueCommand)))
                //    moreCommands.Add(new CommandContextItem(new AddToQueueCommand(_spotifyClient, new PlayerAddToQueueRequest(track.Uri))));
                if (!Without.Contains(typeof(AlbumPage)))
                    moreCommands.Add(new CommandContextItem(new AlbumPage(_spotifyClient, album.Id, album.Name)
                    {
                        Name = String.Format(Resources.ContextMenuResultGoToAlbumTemplate, album.Name)
                    }));
                if (!Without.Contains(typeof(ArtistAlbumsPage)))
                    foreach (SimpleArtist artist in album.Artists)
                    {
                        moreCommands.Add(new CommandContextItem(new ArtistAlbumsPage(_spotifyClient, artist)
                        {
                            Name = String.Format(Resources.ContextMenuResultGoToArtistTemplate, artist.Name)
                        }));
                    }
                return new ListItem(new ResumePlaybackCommand(_spotifyClient, album.Uri))
                {
                    Title = album.Name,
                    Subtitle = Resources.ResultAlbumSubTitle,
                    Icon = For.Count > 25 ? Icons.Play : new IconInfo(album.Images.OrderBy(x => x.Width * x.Height).FirstOrDefault()?.Url),
                    MoreCommands = moreCommands.ToArray()
                };
            });
        }
    }
}