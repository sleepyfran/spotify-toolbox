using Spectre.Console;
using SpotifyAPI.Web;
using SpotifyToolbox.CLI.Commands;

namespace SpotifyToolbox.CLI;

static class Program
{
    static async Task Main(string[] args)
    {
        var clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");

        if (clientId == null)
        {
            AnsiConsole.Markup(
                "[bold red]No client ID found, please specify your client ID  via the SPOTIFY_CLIENT_ID environment variable[/]");
            Environment.Exit(1);
        }

        await ParseArgs(clientId, args);
    }

    static async Task ParseArgs(string clientId, string[] args)
    {
        var command = string.Join(' ', args);
        if (command == "")
        {
            AnsiConsole.Markup("[bold red]No command given[/]");
            Environment.Exit(1);
        }

        var context = await MakeContextAsync(clientId);

        // Only `login` can be executed without credentials.
        if (context is UnauthenticatedContext)
        {
            if (command != "login")
            {
                AnsiConsole.MarkupLine("[yellow]You have to login before executing any command. Run [italic]login[/] to start the auth process[/]");
                return;
            }
            
            await new LoginCommand(context).Execute();
        }
        else
        {
            var authenticatedContext = (context as AuthenticatedContext)!;

            switch (command)
            {
                case "remove albums":
                    await new RemoveAlbumsCommand(authenticatedContext).Execute();
                    break;
                case "remove artists":
                    await new RemoveArtistsCommand(authenticatedContext).Execute();
                    break;
                default:
                    AnsiConsole.Markup("[bold red]Command not recognized[/]");
                    break;
            }
        }
    }

    static async Task<Context> MakeContextAsync(string clientId)
    {
        var authConfig = await Storage.ReadAuthConfigAsync();
        if (authConfig == null)
        {
            return new UnauthenticatedContext(clientId);
        }

        var authenticator = new PKCEAuthenticator(clientId, authConfig.Token);
        authenticator.TokenRefreshed += (sender, token) => Storage.Save(new AuthConfig(token));

        var clientConfig = SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator);
        var client = new SpotifyClient(clientConfig);

        return new AuthenticatedContext(clientId, authConfig, client);
    }
}