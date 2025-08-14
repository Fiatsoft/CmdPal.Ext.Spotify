using CmdPal.Ext.Spotify.Commands;
using CmdPal.Ext.Spotify.Helpers;
using CmdPal.Ext.Spotify.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CmdPal.Ext.Spotify.Pages;

internal sealed partial class ArtistAlbumsPage : ListPage
{
    private readonly SpotifyClient spotifyClient;
    private readonly string? artistId;
    private readonly SimpleAlbum? simpleAlbum;
    private readonly SimpleArtist? simpleArtist;
    private readonly FullArtist? fullArtist;
    private readonly FullAlbum? fullAlbum;
    private readonly string? name;
    private List<SimpleAlbum> items = new();

    private Object resolvedArtist;

    private ArtistAlbumsPage(
        SpotifyClient spotify,
        string? artistId = null,
        FullArtist? fullArtist = null,
        SimpleArtist? simpleArtist = null,
        SimpleAlbum? simpleAlbum = null,
        FullAlbum? fullAlbum = null,
        string? name = null)
    {
        spotifyClient = spotify ?? throw new ArgumentNullException(nameof(spotify));
        this.artistId = artistId;
        this.fullArtist = fullArtist;
        this.simpleArtist = simpleArtist;
        this.simpleAlbum = simpleAlbum;
        this.fullAlbum = fullAlbum;
        this.name = name;

        Title = string.IsNullOrEmpty(name) ? Resources.ArtistAlbumsPageTitle : name;
        Name = Title;
        Icon = Icons.Spotify;
    }

    public ArtistAlbumsPage(SpotifyClient spotify, string artistId, string? name = null)
        : this(spotify, artistId: artistId, null, null, null, name: name) { }

    public ArtistAlbumsPage(SpotifyClient spotify, SimpleArtist artist, string? name = null)
        : this(spotify, artistId: artist.Id, simpleArtist: artist, name: name)
    {
        resolvedArtist = artist;
    }

    public ArtistAlbumsPage(SpotifyClient spotify, FullArtist artist, string? name = null)
        : this(spotify, artistId: artist.Id, fullArtist: artist, name: name)
    {
        resolvedArtist = artist;
    }

    public ArtistAlbumsPage(SpotifyClient spotify, SimpleAlbum album, string? name = null)
        : this(spotify, simpleAlbum: album, name: name) { }

    public ArtistAlbumsPage(SpotifyClient spotify, FullAlbum album, string? name = null)
        : this(spotify, fullAlbum: album, name: name) { }

    public override IListItem[] GetItems()
    {
        try
        {
            var resolvedArtistId = artistId ??
                                   fullArtist?.Id ??
                                   simpleArtist?.Id ??
                                   fullAlbum?.Artists.FirstOrDefault()?.Id ??
                                   simpleAlbum?.Artists.FirstOrDefault()?.Id;

            if (string.IsNullOrEmpty(resolvedArtistId))
                throw new ArgumentException("No artist information provided.");

            if (resolvedArtist == null)
                resolvedArtist = spotifyClient.Artists.Get(resolvedArtistId).GetAwaiter().GetResult();

            this.Icon = resolvedArtist is FullArtist artist ?
                new IconInfo(artist.Images.OrderBy(x => x.Width * x.Height).FirstOrDefault()?.Url) :
                Icons.Spotify;
            
            Title = ((dynamic) resolvedArtist).Name;

            var request = new ArtistsAlbumsRequest
            {
                Limit = 50,
                IncludeGroupsParam = ArtistsAlbumsRequest.IncludeGroups.Album |
                                     ArtistsAlbumsRequest.IncludeGroups.Single
            };

            var firstPage = spotifyClient.Artists.GetAlbums(resolvedArtistId, request).GetAwaiter().GetResult();
            items = spotifyClient.PaginateAll(firstPage).Result.ToList();

            return Album.ListItems(items, spotifyClient, Without: new List<Type> { typeof(ArtistAlbumsPage) }).ToArray();
        }
        catch (Exception ex)
        {
            new ToastStatusMessage(new StatusMessage
            {
                Message = Resources.ErrorArtistPage,
                State = MessageState.Info
            }).Show();

            Journal.Append(
                $"{Resources.ErrorArtistPage}: {ex.Message}: {JsonConvert.SerializeObject(this)}",
                label: Journal.Label.Error);

            return new[]
            {
                new ListItem(new AnonymousCommand(() => { }) { Name = "Go Back", Result = CommandResult.GoBack() })
            };
        }
    }
}
