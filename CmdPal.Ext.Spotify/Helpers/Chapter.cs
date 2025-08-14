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
    internal class Chapter
    {
        internal static IEnumerable<ListItem> ListItems(IList<FullAudiobookChapter> For, SpotifyClient _spotifyClient, IList<Type> Without)
        {
            if (Without == null)
                Without = new List<Type>();
            return For.Where(chapter => chapter != null)
                .Select(chapter =>
                {
                    var moreCommands = new List<CommandContextItem>();
                    if (!Without.Contains(typeof(AddToQueueCommand)))
                        moreCommands.Add(new CommandContextItem(new AddToQueueCommand(_spotifyClient, new PlayerAddToQueueRequest(chapter.Uri))));
                    //if (!Without.Contains(typeof(AlbumPage)))
                    //    moreCommands.Add(new CommandContextItem(new AlbumPage(_spotifyClient, track.Audiobook.Id, track.Audiobook.Name)));
                    return new ListItem(new ResumePlaybackCommand(_spotifyClient, new PlayerResumePlaybackRequest() { Uris = [chapter.Uri] }))
                    {
                        Title = chapter.Name,
                        Subtitle = $"{(chapter.Explicit ? $" • {Resources.ResultSongExplicitSubTitle}" : "")} • {chapter.Audiobook.Name}",
                        Icon = new IconInfo(chapter.Audiobook.Images.OrderBy(x => x.Width * x.Height).FirstOrDefault()?.Url),
                        MoreCommands = moreCommands.ToArray()
                    };
                }).ToList();
        }
    }
}