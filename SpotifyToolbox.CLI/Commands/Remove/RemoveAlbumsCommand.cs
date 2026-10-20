using Spectre.Console;
using Spectre.Console.Cli;
using SpotifyToolbox.CLI.Spotify;

namespace SpotifyToolbox.CLI.Commands.Remove;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class RemoveAlbumsCommand(ISpotifyLibrary library, IAnsiConsole console)
    : AsyncCommand<DryRunSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DryRunSettings settings,
        CancellationToken cancellationToken)
    {
        if (settings.DryRun)
        {
            console.MarkupLine("Running in dry-run mode, nothing will be removed");
        }
        else if (!await console.ConfirmAsync(
                     "[orangered1]Are you absolutely certain? This will remove [bold]ALL[/] the albums in your library and it [bold]CANNOT[/] be undone[/]",
                     defaultValue: false,
                     cancellationToken))
        {
            return 1;
        }

        var albums = await console
            .Status()
            .StartAsync("Finding your albums...", _ => library.GetSavedAlbumsAsync(cancellationToken));

        if (albums.Count == 0)
        {
            console.MarkupLine("[green]There are no albums in your library[/]");
            return 0;
        }

        if (settings.DryRun)
        {
            foreach (var album in albums)
            {
                console.MarkupLine($"{Display.Album(album)} would be removed");
            }

            return 0;
        }

        await console.RunWithProgressAsync(
            $"Removing {albums.Count} albums",
            albums.Count,
            progress => library.RemoveAlbumsAsync(albums, progress, cancellationToken));

        console.MarkupLine($"[green]Removed {albums.Count} albums from your library[/]");
        return 0;
    }
}
