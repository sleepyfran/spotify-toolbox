using Spectre.Console;
using SpotifyAPI.Web;

namespace SpotifyToolbox.CLI.Commands;

public class RemoveAlbumsCommand
{
    private AuthenticatedContext _context;

    public RemoveAlbumsCommand(AuthenticatedContext context)
    {
        _context = context;
    }

    public async Task Execute()
    {
        if (!AnsiConsole.Confirm(
                "[orangered1]Are you absolutely certain? This will remove [bold]ALL[/] the albums in your library and [bold]CANNOT[/] be undone[/]"))
        {
            return;
        }

        AnsiConsole.MarkupLine(
            "[yellow]Spotify currently limits the amount that we can remove albums through the API (currently 20 per request). Get some coffee or popcorn, this might take a while[/]");


        await AnsiConsole
            .Status()
            .StartAsync("Finding your albums...", async ctx =>
            {
                while (true)
                {
                    ctx.Status("Finding your albums...");
                    var albums = await _context.Client.Library.GetAlbums();
                    if (albums.Items!.Count == 0)
                    {
                        AnsiConsole.Markup("[bold green]No more albums found, probably all removed :)[/]");
                        break;
                    }

                    ctx.Status($"Found {albums.Items.Count} albums, removing...");
                    await _context.Client.Library.RemoveAlbums(
                        new LibraryRemoveAlbumsRequest(
                            albums.Items
                                .Select(album => album.Album.Id)
                                .ToList()
                        )
                    );
                }
            });
    }
}