using Spectre.Console;
using Spectre.Console.Cli;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyToolbox.CLI.Auth;
using static SpotifyAPI.Web.Scopes;

namespace SpotifyToolbox.CLI.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class LoginCommand(AppSettings appSettings, IAnsiConsole console) : AsyncCommand
{
    private const int CallbackPort = 8000;
    private static readonly Uri CallbackUri = new($"http://127.0.0.1:{CallbackPort}/callback");

    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        if (appSettings.IsLoggedIn)
        {
            console.MarkupLine("[bold yellow]You're already logged in, skipping[/]");
            return 1;
        }

        using var server = new EmbedIOAuthServer(CallbackUri, CallbackPort);
        await server.Start();

        var authCompletion = new TaskCompletionSource<PKCETokenResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var (verifier, challenge) = PKCEUtil.GenerateCodes();

        server.AuthorizationCodeReceived += async (_, response) =>
        {
            try
            {
                var token = await new OAuthClient().RequestToken(
                    new PKCETokenRequest(appSettings.ClientId, response.Code, server.BaseUri, verifier),
                    cancellationToken);
                authCompletion.TrySetResult(token);
            }
            catch (Exception e)
            {
                authCompletion.TrySetException(e);
            }
        };
        server.ErrorReceived += (_, error, _) =>
        {
            authCompletion.TrySetException(new InvalidOperationException($"Spotify returned an error: {error}"));
            return Task.CompletedTask;
        };

        var request = new LoginRequest(server.BaseUri, appSettings.ClientId, LoginRequest.ResponseType.Code)
        {
            CodeChallenge = challenge,
            CodeChallengeMethod = "S256",
            Scope = [UserFollowRead, UserFollowModify, UserLibraryRead, UserLibraryModify]
        };

        var requestUri = request.ToUri();
        console.MarkupLine(
            "[blue]Attempting to open your browser to authenticate. Please complete the auth process and come back once done[/]");
        try
        {
            BrowserUtil.Open(requestUri);
        }
        catch
        {
            console.MarkupLine("[bold red]Toolbox was unable to open the browser.[/]");
            console.MarkupLine("[blue]Please manually open the following URL:[/]");
            console.WriteLine(requestUri.ToString());
        }

        var token = await authCompletion.Task.WaitAsync(cancellationToken);
        await server.Stop();

        AuthStorage.Save(new AuthConfig(token));
        console.MarkupLine("[bold green]Successfully authenticated! You can now use the rest of commands[/]");
        return 0;
    }
}
