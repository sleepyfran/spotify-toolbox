using Spectre.Console;
using SpotifyAPI.Web;

namespace SpotifyToolbox.CLI.Commands;

public class RemoveArtistsCommand
{
    private AuthenticatedContext _context;

    public RemoveArtistsCommand(AuthenticatedContext context)
    {
        _context = context;
    }

    public async Task Execute()
    {
        if (!AnsiConsole.Confirm(
                "[orangered1]Are you absolutely certain? This will remove [bold]ALL[/] the artists you follow from your library and it [bold]CANNOT[/] be undone[/]"))
        {
            return;
        }

        AnsiConsole.MarkupLine(
            "[yellow]Spotify currently limits the amount that we can remove through the API (currently 50 per request). Get some coffee or popcorn, this might take a while[/]");

        await AnsiConsole
            .Status()
            .StartAsync("Finding your artists...", async ctx =>
            {
                while (true)
                {
                    ctx.Status("Finding your artists...");
                    var artists = await _context.Client.Follow.OfCurrentUser();
                    if (artists.Artists.Items!.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[bold green]No more artists found, probably all removed :)[/]");
                        break;
                    }

                    ctx.Status($"Found {artists.Artists.Items.Count} artists, removing...");
                    await _context.Client.Follow.Unfollow(
                        new UnfollowRequest(
                            UnfollowRequest.Type.Artist,
                            artists.Artists.Items.Select(artist => artist.Id).ToList()
                        )
                    );
                }
            });
    }
}