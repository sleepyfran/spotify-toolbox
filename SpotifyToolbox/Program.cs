using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using SpotifyAPI.Web;
using SpotifyToolbox.CLI.Commands;
using SpotifyToolbox.CLI.Commands.Remove;
using SpotifyToolbox.CLI.Infrastructure;

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

        var appContext = await MakeContextAsync(clientId);
        var dependencyRegistrar = CreateDependencyContainer(appContext);
        var app = new CommandApp(dependencyRegistrar);
        app.Configure(config =>
        {
            config
                .AddCommand<LoginCommand>("login")
                .WithDescription("Starts the authentication process needed for the rest of the commands");

            // Only add commands if user is authenticated.
            if (appContext is AuthenticatedAppContext)
            {
                config
                    .AddBranch<RemoveSettings>("remove", branch =>
                    {
                        branch.SetDescription("Provides a way to wipe all albums or artists from your library");

                        branch.AddCommand<RemoveAlbumsCommand>("albums")
                            .WithDescription("Removes all the albums on your library");
                        branch.AddCommand<RemoveArtistsCommand>("artists")
                            .WithDescription("Unfollows all the artists on your library.");
                    });
            }
        });

        await app.RunAsync(args);
    }

    static async Task<AppContext> MakeContextAsync(string clientId)
    {
        var authConfig = await Storage.ReadAuthConfigAsync();
        if (authConfig == null)
        {
            return new UnauthenticatedAppContext(clientId);
        }

        var authenticator = new PKCEAuthenticator(clientId, authConfig.Token);
        authenticator.TokenRefreshed += (sender, token) => Storage.Save(new AuthConfig(token));

        var clientConfig = SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator);
        var client = new SpotifyClient(clientConfig);

        return new AuthenticatedAppContext(clientId, authConfig, client);
    }

    static TypeRegistrar CreateDependencyContainer(AppContext context)
    {
        var registrations = new ServiceCollection();
        registrations.AddSingleton(context);
        return new TypeRegistrar(registrations);
    }
}