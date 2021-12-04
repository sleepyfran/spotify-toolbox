using Spectre.Console;
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
        var command = args.FirstOrDefault();
        if (command == null)
        {
            AnsiConsole.Markup("[bold red]No command given[/]");
            Environment.Exit(1);
        }

        var authConfig = await Storage.ReadAuthConfig();
        var appConfig = new AppConfig(clientId, authConfig);
        
        switch (command)
        {
            case "login":
                await new LoginCommand(appConfig).Execute();
                break;
            default:
                AnsiConsole.Markup("[bold red]Command not recognized[/]");
                break;
        }
    }
}