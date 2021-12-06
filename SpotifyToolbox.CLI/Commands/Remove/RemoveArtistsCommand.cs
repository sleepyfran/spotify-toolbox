using Spectre.Console;
using Spectre.Console.Cli;
using SpotifyAPI.Web;

namespace SpotifyToolbox.CLI.Commands.Remove;

// ReSharper disable once ClassNeverInstantiated.Global
public class RemoveArtistsCommand : AsyncCommand<RemoveSettings>
{
    private readonly AuthenticatedAppContext _appContext;

    public RemoveArtistsCommand(AppContext appContext)
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
                    "[orangered1]Are you absolutely certain? This will remove [bold]ALL[/] the artists you follow from your library and it [bold]CANNOT[/] be undone[/]"))
            {
                return 1;
            }
        }

        AnsiConsole.MarkupLine(
            "[yellow]Spotify currently limits the amount that we can remove through the API (currently 50 per request). Get some coffee or popcorn, this might take a while[/]");

        await AnsiConsole
            .Status()
            .StartAsync("Finding your artists...", async ctx =>
            {
                ctx.Status("Finding your artists...");
                var initialArtists = await _appContext.Client.Follow.OfCurrentUser();
                await FetchAndRemove(ctx, settings, initialArtists);
            });

        return 0;
    }

    private async Task FetchAndRemove(StatusContext context, RemoveSettings settings, FollowedArtistsResponse? response)
    {
        if (response == null)
        {
            AnsiConsole.MarkupLine("[green]No more artists to process in your library[/]");
            return;
        }

        await Remove(settings, response.Artists.Items);

        context.Status("Finding more artists...");
        if (response.Artists.Next != null)
        {
            var nextPage = await _appContext.Client.NextPage(response.Artists);
            await FetchAndRemove(context, settings, nextPage);
        }
        else
        {
            await FetchAndRemove(context, settings, null);
        }
    }

    private async Task Remove(RemoveSettings settings, List<FullArtist> artists)
    {
        if (settings.DryRun)
        {
            artists.ForEach(artist => { AnsiConsole.MarkupLine($"[bold]{artist.Name}[/] would be unfollowed"); });
        }
        else
        {
            await _appContext.Client.Follow.Unfollow(
                new UnfollowRequest(
                    UnfollowRequest.Type.Artist,
                    artists.Select(artist => artist.Id).ToList()
                )
            );
            artists.ForEach(artist => { AnsiConsole.MarkupLine($"[bold]{artist.Name}[/] unfollowed"); });
        }
    }
}