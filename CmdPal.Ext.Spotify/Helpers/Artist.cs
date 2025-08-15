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
    internal class Artist
    {
        internal static IEnumerable<ListItem> ListItems(IList<FullArtist> For, SpotifyClient _spotifyClient, IList<Type> Without = null)
        {
            if (Without == null)
                Without = new List<Type>();
            return For.Where(artist => artist != null).Select(artist => {
                var moreCommands = new List<CommandContextItem>();
                if (!Without.Contains(typeof(ArtistAlbumsPage)))
                    moreCommands.Add(new CommandContextItem(new ArtistAlbumsPage(_spotifyClient, artist)
                    {
                        Name = String.Format(Resources.ContextMenuResultGoToArtistTemplate, artist.Name)
                    }));
                return new ListItem(new ResumePlaybackCommand(_spotifyClient, artist.Uri))
                {
                    Title = artist.Name,
                    Subtitle = Resources.ResultArtistSubTitle,
                    Icon = For.Count > 25 ? Icons.Play : new IconInfo(artist.Images.OrderBy(x => x.Width * x.Height).FirstOrDefault()?.Url),
                    MoreCommands = moreCommands.ToArray()
                };
            });
        }
    }
}