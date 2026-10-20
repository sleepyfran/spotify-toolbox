using Spectre.Console;
using Spectre.Console.Cli;
using SpotifyToolbox.CLI.Auth;

namespace SpotifyToolbox.CLI.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class LogoutCommand(IAnsiConsole console) : Command
{
    public override int Execute(CommandContext context, CancellationToken cancellationToken)
    {
        if (!AuthStorage.Delete())
        {
            console.MarkupLine("[yellow]You're not logged in, skipping[/]");
            return 1;
        }

        console.MarkupLine("[green]Logged out successfully[/]");
        return 0;
    }
}
