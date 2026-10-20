using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using SpotifyAPI.Web;
using SpotifyToolbox.CLI;
using SpotifyToolbox.CLI.Auth;
using SpotifyToolbox.CLI.Commands;
using SpotifyToolbox.CLI.Commands.Remove;
using SpotifyToolbox.CLI.Infrastructure;
using SpotifyToolbox.CLI.Spotify;

var clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
if (string.IsNullOrWhiteSpace(clientId))
{
    AnsiConsole.MarkupLine(
        "[bold red]No client ID found, please specify your client ID via the SPOTIFY_CLIENT_ID environment variable[/]");
    return 1;
}

var authConfig = await AuthStorage.ReadAsync();
var isLoggedIn = authConfig != null;

var services = new ServiceCollection();
services.AddSingleton(AnsiConsole.Console);
services.AddSingleton(new AppSettings(clientId, isLoggedIn));

if (authConfig != null)
{
    services.AddSingleton(CreateSpotifyClient(clientId, authConfig));
    services.AddSingleton<ISpotifyLibrary, SpotifyLibrary>();
}

var app = new CommandApp(new TypeRegistrar(services));
app.Configure(config =>
{
    config.SetApplicationName("spotify-toolbox");
    config.SetExceptionHandler((exception, _) => HandleException(exception));

    config
        .AddCommand<LoginCommand>("login")
        .WithDescription("Starts the authentication process needed for the rest of the commands");
    config
        .AddCommand<LogoutCommand>("logout")
        .WithDescription("Removes the stored Spotify credentials");

    // The rest of commands need access to the user's library.
    if (!isLoggedIn)
    {
        return;
    }

    config.AddBranch<DryRunSettings>("remove", branch =>
    {
        branch.SetDescription("Provides a way to wipe all albums or artists from your library");

        branch
            .AddCommand<RemoveAlbumsCommand>("albums")
            .WithDescription("Removes all the albums in your library");
        branch
            .AddCommand<RemoveArtistsCommand>("artists")
            .WithDescription("Unfollows all the artists in your library");
    });
});

return await app.RunAsync(args);

static ISpotifyClient CreateSpotifyClient(string clientId, AuthConfig authConfig)
{
    var authenticator = new PKCEAuthenticator(clientId, authConfig.Token);
    authenticator.TokenRefreshed += (_, token) => AuthStorage.Save(new AuthConfig(token));

    var clientConfig = SpotifyClientConfig
        .CreateDefault()
        .WithAuthenticator(authenticator)
        .WithRetryHandler(new SimpleRetryHandler());

    return new SpotifyClient(clientConfig);
}

static int HandleException(Exception exception)
{
    switch (exception)
    {
        case CommandAppException { Pretty: { } pretty }:
            AnsiConsole.Write(pretty);
            break;
        case APIUnauthorizedException:
            AnsiConsole.MarkupLine(
                "[bold red]Spotify rejected your credentials.[/] Run [blue]logout[/] and then [blue]login[/] to authenticate again");
            break;
        case APIException apiException:
            AnsiConsole.MarkupLineInterpolated(
                $"[bold red]Spotify returned an error ({apiException.Response?.StatusCode}):[/] {apiException.Message}");
            break;
        case OperationCanceledException:
            AnsiConsole.MarkupLine("[yellow]Operation cancelled[/]");
            break;
        default:
            AnsiConsole.WriteException(exception, ExceptionFormats.ShortenEverything);
            break;
    }

    return -1;
}
