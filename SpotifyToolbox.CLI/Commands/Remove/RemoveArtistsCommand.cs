using Spectre.Console;
using Spectre.Console.Cli;
using SpotifyToolbox.CLI.Spotify;

namespace SpotifyToolbox.CLI.Commands.Remove;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class RemoveArtistsCommand(ISpotifyLibrary library, IAnsiConsole console)
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
                     "[orangered1]Are you absolutely certain? This will remove [bold]ALL[/] the artists you follow from your library and it [bold]CANNOT[/] be undone[/]",
                     defaultValue: false,
                     cancellationToken))
        {
            return 1;
        }

        var artists = await console
            .Status()
            .StartAsync("Finding your artists...", _ => library.GetFollowedArtistsAsync(cancellationToken));

        if (artists.Count == 0)
        {
            console.MarkupLine("[green]You don't follow any artist[/]");
            return 0;
        }

        if (settings.DryRun)
        {
            foreach (var artist in artists)
            {
                console.MarkupLine($"{Display.Artist(artist)} would be unfollowed");
            }

            return 0;
        }

        await console.RunWithProgressAsync(
            $"Unfollowing {artists.Count} artists",
            artists.Count,
            progress => library.UnfollowArtistsAsync(artists, progress, cancellationToken));

        console.MarkupLine($"[green]Unfollowed {artists.Count} artists[/]");
        return 0;
    }
}
