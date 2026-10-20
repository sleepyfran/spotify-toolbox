using Spectre.Console.Testing;
using SpotifyToolbox.CLI.Commands.Cleanup;
using SpotifyToolbox.CLI.Prompts;
using SpotifyToolbox.CLI.Spotify;
using static SpotifyToolbox.Tests.TestData;

namespace SpotifyToolbox.Tests;

public class TreeSelectionPromptTests
{
    private readonly TestConsole _console = new TestConsole().Interactive();

    // Groups are sorted by name: Beck, Björk, Radiohead, Thom Yorke.
    private readonly IReadOnlyList<TreeGroup<Artist, Album>> _groups = LibraryCleanup.GroupByArtist(Library);

    private Task<IReadOnlyList<TreeGroup<Artist, Album>>?> ShowAsync(params ConsoleKey[] keys)
    {
        _console.Profile.Height = 30;
        foreach (var key in keys)
        {
            _console.Input.PushKey(key);
        }

        var prompt = new TreeSelectionPrompt<Artist, Album>(_groups, artist => artist.Name, album => album.Name);
        return prompt.ShowAsync(_console, TestContext.Current.CancellationToken);
    }

    private TreeGroup<Artist, Album> Group(Artist artist) => _groups.Single(group => group.Value == artist);

    [Fact]
    public async Task SpaceOnAnArtistSelectsAllOfItsAlbums()
    {
        var result = await ShowAsync(ConsoleKey.DownArrow, ConsoleKey.Spacebar, ConsoleKey.Enter);

        Assert.NotNull(result);
        Assert.Equal(SelectionState.All, Group(Bjork).State);
        Assert.Equal([Homogenic, Collaboration], result.SelectMany(group => group.SelectedItems));
    }

    [Fact]
    public async Task ExpandingAnArtistAllowsSelectingSpecificAlbums()
    {
        await ShowAsync(
            ConsoleKey.DownArrow,
            ConsoleKey.DownArrow,
            ConsoleKey.RightArrow, // Expands Radiohead and moves to The Bends.
            ConsoleKey.DownArrow,
            ConsoleKey.Spacebar, // Selects OK Computer.
            ConsoleKey.LeftArrow, // Collapses back to Radiohead.
            ConsoleKey.Enter);

        var radiohead = Group(Radiohead);
        Assert.Equal(SelectionState.Partial, radiohead.State);
        Assert.False(radiohead.IsExpanded);
        Assert.Equal([OkComputer], radiohead.SelectedItems);
        Assert.Contains("[-] Radiohead", _console.Output);
    }

    [Fact]
    public async Task CursorStaysOnTheArtistAfterCollapsing()
    {
        await ShowAsync(
            ConsoleKey.DownArrow,
            ConsoleKey.RightArrow,
            ConsoleKey.LeftArrow,
            ConsoleKey.DownArrow, // Moves to Radiohead, not to one of Björk's albums.
            ConsoleKey.Spacebar,
            ConsoleKey.Enter);

        Assert.Equal(SelectionState.None, Group(Bjork).State);
        Assert.Equal(SelectionState.All, Group(Radiohead).State);
    }

    [Fact]
    public async Task SpaceOnAPartiallySelectedArtistSelectsTheRest()
    {
        await ShowAsync(
            ConsoleKey.DownArrow,
            ConsoleKey.DownArrow,
            ConsoleKey.RightArrow,
            ConsoleKey.Spacebar,
            ConsoleKey.LeftArrow,
            ConsoleKey.Spacebar,
            ConsoleKey.Enter);

        Assert.Equal(SelectionState.All, Group(Radiohead).State);
    }

    [Fact]
    public async Task TypingALetterJumpsToTheNextArtistStartingWithIt()
    {
        _console.Input.PushCharacter('t');
        var result = await ShowAsync(ConsoleKey.Spacebar, ConsoleKey.Enter);

        Assert.NotNull(result);
        Assert.Equal([Eraser], result.SelectMany(group => group.SelectedItems));
    }

    [Fact]
    public async Task EscapeCancelsThePrompt()
    {
        var result = await ShowAsync(ConsoleKey.Spacebar, ConsoleKey.Escape);

        Assert.Null(result);
    }

    [Fact]
    public async Task CursorDoesNotGoPastTheEnds()
    {
        await ShowAsync(
            ConsoleKey.UpArrow,
            ConsoleKey.Spacebar,
            ConsoleKey.End,
            ConsoleKey.DownArrow,
            ConsoleKey.Spacebar,
            ConsoleKey.Enter);

        Assert.Equal(SelectionState.All, Group(Beck).State);
        Assert.Equal(SelectionState.All, Group(Thom).State);
    }

    [Fact]
    public async Task FailsInNonInteractiveTerminals()
    {
        var prompt = new TreeSelectionPrompt<Artist, Album>(_groups, artist => artist.Name, album => album.Name);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => prompt.ShowAsync(new TestConsole(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ScrollsWhenThereAreMoreRowsThanFitInAPage()
    {
        var prompt = new TreeSelectionPrompt<Artist, Album>(_groups, artist => artist.Name, album => album.Name)
        {
            PageSize = 2
        };
        _console.Input.PushKey(ConsoleKey.End);
        _console.Input.PushKey(ConsoleKey.Enter);

        await prompt.ShowAsync(_console, TestContext.Current.CancellationToken);

        Assert.Contains("Showing 3-4 of 4", _console.Output);
    }
}
