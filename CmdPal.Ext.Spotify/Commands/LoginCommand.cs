using CmdPal.Ext.Spotify.Helpers;
using CmdPal.Ext.Spotify.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace CmdPal.Ext.Spotify.Commands;

internal partial class LoginCommand : InvokableCommand
{
    private string _clientId;
    private string _credentialsPath;
    public event EventHandler? LoggedIn;

    public LoginCommand(
        string clientId,
        string credentialsPath
    )
    {
        _clientId = clientId;
        _credentialsPath = credentialsPath;
    }

    public override CommandResult Invoke()
    {
        var _ = InvokeAsync();
        new ToastStatusMessage(new StatusMessage() { Message = Resources.LoginPromptToast, State = MessageState.Info }).Show();
        return CommandResult.KeepOpen();
    }

    private async Task InvokeAsync()
    {
        if (SpotifyCommandsProvider.EmbedIOAuthServer != null)
        {
            await SpotifyCommandsProvider.EmbedIOAuthServer.Stop();
            SpotifyCommandsProvider.EmbedIOAuthServer.Dispose();
            SpotifyCommandsProvider.EmbedIOAuthServer = null;
        }
        var (verifier, challenge) = PKCEUtil.GenerateCodes();

        var tcs = new TaskCompletionSource();

        var callbackUri = new Uri("http://127.0.0.1:5543/callback");
        var authServer = SpotifyCommandsProvider.EmbedIOAuthServer = new EmbedIOAuthServer(callbackUri, 5543);

        authServer.AuthorizationCodeReceived += async (sender, response) =>
        {
            await authServer.Stop();
            var tokenRequest = new PKCETokenRequest(_clientId, response.Code, authServer.BaseUri, verifier);
            var client = new OAuthClient();
            var tokenResponse = await client.RequestToken(tokenRequest);

            Directory.CreateDirectory(Path.GetDirectoryName(_credentialsPath));
            await File.WriteAllTextAsync(_credentialsPath, JsonConvert.SerializeObject(tokenResponse));

            LoggedIn?.Invoke(this, EventArgs.Empty);

            tcs.SetResult();
        };

        await authServer.Start();

        var loginRequest = new LoginRequest(authServer.BaseUri, _clientId, LoginRequest.ResponseType.Code)
        {
            CodeChallenge = challenge,
            CodeChallengeMethod = "S256",
            Scope = new List<string>
            {
                Scopes.UserReadPlaybackState,
                Scopes.UserModifyPlaybackState,
                Scopes.UserReadCurrentlyPlaying,
                Scopes.UserTopRead,
                Scopes.UserReadEmail
            }
        };

        try
        {
            BrowserUtil.Open(loginRequest.ToUri());
        }
        catch (Exception ex)
        {
            new ToastStatusMessage(new StatusMessage() { Message = Resources.ErrorLoginToast, State = MessageState.Error }).Show();
            Journal.Append($"{Resources.ResourceManager.GetString("ErrorLoginToast", CultureInfo.InvariantCulture)}: {ex.Message}", label: Journal.Label.Error);
            return;
        }

        await tcs.Task;
    }
}