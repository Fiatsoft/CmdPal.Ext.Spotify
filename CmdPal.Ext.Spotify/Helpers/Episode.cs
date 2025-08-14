using CmdPal.Ext.Spotify.Commands;
using CmdPal.Ext.Spotify.Pages;
using CmdPal.Ext.Spotify.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using SpotifyAPI.Web;
using Swan;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CmdPal.Ext.Spotify.Helpers
{
    internal class Episode
    {
        internal static IEnumerable<ListItem> ListItems(IList<SimpleEpisode> For, SpotifyClient _spotifyClient, IList<Type> Without = null)
        {
            if (Without == null)
                Without = new List<Type>();
            return For.Where(episode => episode != null)
                .Select(episode =>
                {
                    var moreCommands = new List<CommandContextItem>();
                    if (!Without.Contains(typeof(AddToQueueCommand)))
                        moreCommands.Add(new CommandContextItem(new AddToQueueCommand(_spotifyClient, new PlayerAddToQueueRequest(episode.Uri))));
                    if (!Without.Contains(typeof(ShowPage)))
                        moreCommands.Add(new CommandContextItem(new ShowPage(_spotifyClient, episode) { Name = Resources.ContextMenuResultGoToShowTitle })); 
                    return new ListItem(new ResumePlaybackCommand(_spotifyClient, new PlayerResumePlaybackRequest() { Uris = [episode.Uri] }))
                    {
                        Title = episode.Name,
                        Subtitle = $"Episode{(episode.Explicit ? $" • {Resources.ResultSongExplicitSubTitle}" : "")} • {episode.Description.Truncate(100)}",
                        Icon = new IconInfo(episode.Images.OrderBy(x => x.Width * x.Height).FirstOrDefault()?.Url),
                        MoreCommands = moreCommands.ToArray()
                    };
                }).ToList();
        }
    }
}