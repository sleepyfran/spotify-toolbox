using Spectre.Console;
using Spectre.Console.Cli;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using static SpotifyAPI.Web.Scopes;

namespace SpotifyToolbox.CLI.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
public class LoginCommand : AsyncCommand
{
    private readonly AppContext _appContext;

    public LoginCommand(AppContext appContext)
    {
        _appContext = appContext;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        if (_appContext is AuthenticatedAppContext)
        {
            AnsiConsole.Markup("[bold yellow]You're already logged in, skipping[/]");
            return 1;
        }

        var server = new EmbedIOAuthServer(new Uri("http://localhost:8000/callback"), 8000);
        await server.Start();

        var authCompletionTask = new TaskCompletionSource();

        var (verifier, challenge) = PKCEUtil.GenerateCodes();

        server.AuthorizationCodeReceived += async (sender, response) =>
        {
            await server.Stop();
            var token = await new OAuthClient().RequestToken(
                new PKCETokenRequest(_appContext.ClientId, response.Code, server.BaseUri, verifier)
            );

            AnsiConsole.Markup("[bold green]Successfully authenticated! You can now use the rest of commands[/]");
            Storage.Save(new AuthConfig(token));
            authCompletionTask.SetResult();
        };

        var request = new LoginRequest(server.BaseUri, _appContext.ClientId, LoginRequest.ResponseType.Code)
        {
            CodeChallenge = challenge,
            CodeChallengeMethod = "S256",
            Scope = new List<string>
                {UserFollowRead, UserFollowModify, UserReadEmail, UserLibraryRead, UserLibraryModify}
        };

        var requestUri = request.ToUri();
        try
        {
            AnsiConsole.Markup(
                "[blue]Attempting to open your browser to authenticate. Please complete the auth process and come back once done[/]");
            BrowserUtil.Open(requestUri);
        }
        catch
        {
            AnsiConsole.Markup("[bold red]Toolbox was unable to open the browser.[/]");
            AnsiConsole.Markup("[blue]Please manually open the following URL:[/]");
            AnsiConsole.Markup(requestUri.ToString());
        }

        await authCompletionTask.Task;
        return 0;
    }
}