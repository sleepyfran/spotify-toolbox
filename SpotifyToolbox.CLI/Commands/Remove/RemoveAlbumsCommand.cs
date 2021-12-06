using Spectre.Console;
using Spectre.Console.Cli;
using SpotifyAPI.Web;

namespace SpotifyToolbox.CLI.Commands.Remove;

// ReSharper disable once ClassNeverInstantiated.Global
public class RemoveAlbumsCommand : AsyncCommand<RemoveSettings>
{
    private readonly AuthenticatedAppContext _appContext;

    public RemoveAlbumsCommand(AppContext appContext)
    {
        _appContext = appContext as AuthenticatedAppContext ??
                      throw new InvalidOperationException("Command can only be called when authenticated");
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RemoveSettings settings)
    {
        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("Running in dry-run mode, nothing will be removed");
        }
        else
        {
            if (!AnsiConsole.Confirm(
                    "[orangered1]Are you absolutely certain? This will remove [bold]ALL[/] the albums in your library and it [bold]CANNOT[/] be undone[/]"))
            {
                return 1;
            }
        }

        AnsiConsole.MarkupLine(
            "[yellow]Spotify currently limits the amount that we can remove through the API (currently 20 per request). Get some coffee or popcorn, this might take a while[/]");

        await AnsiConsole
            .Status()
            .StartAsync("Finding your albums...", async ctx =>
            {
                ctx.Status("Finding your albums...");
                var initialAlbums = await _appContext.Client.Library.GetAlbums();
                await FetchAndRemove(ctx, settings, initialAlbums);
            });

        return 0;
    }

    private async Task FetchAndRemove(StatusContext context, RemoveSettings settings, Paging<SavedAlbum>? albums)
    {
        if (albums == null)
        {
            AnsiConsole.MarkupLine("[green]No more albums to process in your library[/]");
            return;
        }

        await Remove(settings, albums.Items);

        context.Status("Finding more albums...");
        if (albums.Next != null)
        {
            var nextPage = await _appContext.Client.NextPage(albums);
            await FetchAndRemove(context, settings, nextPage);
        }
        else
        {
            await FetchAndRemove(context, settings, null);
        }
    }

    private async Task Remove(RemoveSettings settings, List<SavedAlbum> albums)
    {
        if (settings.DryRun)
        {
            albums.ForEach(item =>
            {
                AnsiConsole.MarkupLine(
                    $"[bold]{item.Album.Name}[/] by [bold]{item.Album.Artists.First().Name}[/] would be removed");
            });
        }
        else
        {
            await _appContext.Client.Library.RemoveAlbums(
                new LibraryRemoveAlbumsRequest(
                    albums
                        .Select(album => album.Album.Id)
                        .ToList()
                )
            );
            albums.ForEach(item =>
            {
                AnsiConsole.MarkupLine(
                    $"[bold]{item.Album.Name}[/] by [bold]{item.Album.Artists.First().Name}[/] removed");
            });
        }
    }
}