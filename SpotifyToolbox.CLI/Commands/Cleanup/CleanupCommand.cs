using Spectre.Console;
using Spectre.Console.Cli;
using SpotifyToolbox.CLI.Prompts;
using SpotifyToolbox.CLI.Spotify;

namespace SpotifyToolbox.CLI.Commands.Cleanup;

/// <summary>
/// Interactively removes albums from the library, grouped by artist, and optionally unfollows the
/// artists that end up without any album in the library.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class CleanupCommand(ISpotifyLibrary library, IAnsiConsole console) : AsyncCommand<DryRunSettings>
{
    private const string KeepFollowing = "Keep following all of them";
    private const string UnfollowAll = "Unfollow all of them";
    private const string ChooseToUnfollow = "Let me choose which ones to unfollow";

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DryRunSettings settings,
        CancellationToken cancellationToken)
    {
        if (settings.DryRun)
        {
            console.MarkupLine("Running in dry-run mode, nothing will be removed");
        }

        var albums = await console
            .Status()
            .StartAsync("Fetching the albums in your library...", _ => library.GetSavedAlbumsAsync(cancellationToken));

        if (albums.Count == 0)
        {
            console.MarkupLine("[green]There are no albums in your library[/]");
            return 0;
        }

        var groups = LibraryCleanup.GroupByArtist(albums);
        var prompt = new TreeSelectionPrompt<Artist, Album>(groups, artist => artist.Name, FormatAlbum)
        {
            Title = $"Select the albums to remove ({albums.Count} albums from {groups.Count} artists)",
            ItemsName = "album(s)",
            GroupsName = "artist(s)"
        };

        if (await prompt.ShowAsync(console, cancellationToken) is null)
        {
            console.MarkupLine("[yellow]Cleanup cancelled, nothing was removed[/]");
            return 1;
        }

        var albumsToRemove = groups.SelectMany(group => group.SelectedItems).ToList();
        if (albumsToRemove.Count == 0)
        {
            console.MarkupLine("[yellow]No albums selected, nothing to do[/]");
            return 0;
        }

        var artistsToUnfollow = await AskArtistsToUnfollowAsync(albums, albumsToRemove, cancellationToken);

        console.Write(Summary(groups, artistsToUnfollow));

        if (settings.DryRun)
        {
            console.MarkupLine("[grey]Dry-run mode: nothing was changed[/]");
            return 0;
        }

        if (!await console.ConfirmAsync(
                "[orangered1]Do you want to proceed? This [bold]CANNOT[/] be undone[/]",
                defaultValue: false,
                cancellationToken))
        {
            console.MarkupLine("[yellow]Cleanup cancelled, nothing was removed[/]");
            return 1;
        }

        await console.RunWithProgressAsync(
            $"Removing {albumsToRemove.Count} albums",
            albumsToRemove.Count,
            progress => library.RemoveAlbumsAsync(albumsToRemove, progress, cancellationToken));

        if (artistsToUnfollow.Count > 0)
        {
            await console.RunWithProgressAsync(
                $"Unfollowing {artistsToUnfollow.Count} artists",
                artistsToUnfollow.Count,
                progress => library.UnfollowArtistsAsync(artistsToUnfollow, progress, cancellationToken));
        }

        console.MarkupLine(
            $"[green]Done! Removed {albumsToRemove.Count} albums and unfollowed {artistsToUnfollow.Count} artists[/]");
        return 0;
    }

    /// <summary>
    /// Finds the followed artists that won't have any albums left in the library after the
    /// cleanup and asks the user whether to keep following them.
    /// </summary>
    private async Task<IReadOnlyList<Artist>> AskArtistsToUnfollowAsync(
        IReadOnlyList<Album> albums,
        IReadOnlyList<Album> albumsToRemove,
        CancellationToken cancellationToken)
    {
        var artistsWithoutAlbums = LibraryCleanup.FindArtistsWithoutAlbums(albums, albumsToRemove);
        if (artistsWithoutAlbums.Count == 0)
        {
            return [];
        }

        var followedArtists = await console
            .Status()
            .StartAsync(
                "Checking which artists you follow...",
                _ => library.FilterFollowedAsync(artistsWithoutAlbums, cancellationToken));

        if (followedArtists.Count == 0)
        {
            return [];
        }

        console.MarkupLine(
            $"You follow {followedArtists.Count} artist(s) that won't have any albums left in your library:");
        foreach (var artist in followedArtists)
        {
            console.MarkupLine($"  • {Display.Artist(artist)}");
        }

        var choice = await console.PromptAsync(
            new SelectionPrompt<string>()
                .Title("Do you want to keep following them?")
                .AddChoices(KeepFollowing, UnfollowAll, ChooseToUnfollow),
            cancellationToken);

        return choice switch
        {
            UnfollowAll => followedArtists,
            ChooseToUnfollow => await console.PromptAsync(
                new MultiSelectionPrompt<Artist>()
                    .Title("Which artists do you want to unfollow?")
                    .NotRequired()
                    .PageSize(15)
                    .InstructionsText(
                        "[grey](Press [blue]<space>[/] to toggle an artist, [green]<enter>[/] to accept)[/]")
                    .UseConverter(artist => Markup.Escape(artist.Name))
                    .AddChoices(followedArtists),
                cancellationToken),
            _ => []
        };
    }

    private static Tree Summary(IEnumerable<TreeGroup<Artist, Album>> groups, IReadOnlyList<Artist> artistsToUnfollow)
    {
        var tree = new Tree("[bold]Summary of changes[/]");

        var albumsNode = tree.AddNode("[bold]Albums to remove[/]");
        foreach (var group in groups.Where(group => group.SelectedCount > 0))
        {
            var label = group.State == SelectionState.All
                ? $"{Display.Artist(group.Value)} [grey](all {group.Items.Count} albums)[/]"
                : $"{Display.Artist(group.Value)} [grey]({group.SelectedCount} of {group.Items.Count} albums)[/]";

            var artistNode = albumsNode.AddNode(label);
            foreach (var album in group.SelectedItems)
            {
                artistNode.AddNode(Markup.Escape(FormatAlbum(album)));
            }
        }

        if (artistsToUnfollow.Count > 0)
        {
            var artistsNode = tree.AddNode("[bold]Artists to unfollow[/]");
            foreach (var artist in artistsToUnfollow)
            {
                artistsNode.AddNode(Display.Artist(artist));
            }
        }

        return tree;
    }

    private static string FormatAlbum(Album album) =>
        string.IsNullOrEmpty(album.ReleaseYear) ? album.Name : $"{album.Name} ({album.ReleaseYear})";
}
