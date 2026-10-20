using Spectre.Console.Cli;
using Spectre.Console.Testing;
using SpotifyToolbox.CLI.Commands;
using SpotifyToolbox.CLI.Commands.Cleanup;
using SpotifyToolbox.CLI.Spotify;
using static SpotifyToolbox.Tests.TestData;

namespace SpotifyToolbox.Tests;

public class CleanupCommandTests
{
    private readonly TestConsole _console = new TestConsole().Interactive();
    private readonly FakeSpotifyLibrary _library = new(Library, followed: [Radiohead, Bjork, Beck]);

    private Task<int> RunAsync(bool dryRun = false)
    {
        _console.Profile.Height = 30;
        var command = new CleanupCommand(_library, _console);
        var context = new CommandContext([], new NoRemainingArguments(), "cleanup", null);
        return command.ExecuteAsync(context, new DryRunSettings { DryRun = dryRun }, TestContext.Current.CancellationToken);
    }

    // Artists are sorted by name: Beck, Björk, Radiohead, Thom Yorke.
    private void SelectRadioheadAndOneBjorkAlbum()
    {
        _console.Input.PushKey(ConsoleKey.DownArrow);
        _console.Input.PushKey(ConsoleKey.RightArrow); // Expands Björk, cursor on Homogenic.
        _console.Input.PushKey(ConsoleKey.Spacebar);
        _console.Input.PushKey(ConsoleKey.LeftArrow);
        _console.Input.PushKey(ConsoleKey.DownArrow);
        _console.Input.PushKey(ConsoleKey.Spacebar); // Selects all of Radiohead.
        _console.Input.PushKey(ConsoleKey.Enter);
    }

    [Fact]
    public async Task RemovesSelectedAlbumsAndUnfollowsArtistsLeftWithoutAlbums()
    {
        SelectRadioheadAndOneBjorkAlbum();
        _console.Input.PushKey(ConsoleKey.DownArrow); // "Unfollow all of them"
        _console.Input.PushKey(ConsoleKey.Enter);
        _console.Input.PushTextWithEnter("y");

        var exitCode = await RunAsync();

        Assert.Equal(0, exitCode);
        Assert.Equal([Homogenic, TheBends, OkComputer, KidA], _library.RemovedAlbums);
        // Björk still has an album in the library, so only Radiohead is unfollowed.
        Assert.Equal([Radiohead], _library.UnfollowedArtists);
    }

    [Fact]
    public async Task KeepsFollowingArtistsIfTheUserWantsTo()
    {
        SelectRadioheadAndOneBjorkAlbum();
        _console.Input.PushKey(ConsoleKey.Enter); // "Keep following all of them"
        _console.Input.PushTextWithEnter("y");

        await RunAsync();

        Assert.Equal(4, _library.RemovedAlbums.Count);
        Assert.Empty(_library.UnfollowedArtists);
    }

    [Fact]
    public async Task DoesNotAskAboutArtistsThatAreNotFollowed()
    {
        _console.Input.PushKey(ConsoleKey.End); // Thom Yorke, who isn't followed.
        _console.Input.PushKey(ConsoleKey.Spacebar);
        _console.Input.PushKey(ConsoleKey.Enter);
        _console.Input.PushTextWithEnter("y");

        await RunAsync();

        Assert.Equal([Eraser], _library.RemovedAlbums);
        Assert.Empty(_library.UnfollowedArtists);
        Assert.DoesNotContain("keep following", _console.Output);
    }

    [Fact]
    public async Task DoesNothingInDryRunMode()
    {
        SelectRadioheadAndOneBjorkAlbum();
        _console.Input.PushKey(ConsoleKey.DownArrow);
        _console.Input.PushKey(ConsoleKey.Enter);

        var exitCode = await RunAsync(dryRun: true);

        Assert.Equal(0, exitCode);
        Assert.Empty(_library.RemovedAlbums);
        Assert.Empty(_library.UnfollowedArtists);
        Assert.Contains("Artists to unfollow", _console.Output);
    }

    [Fact]
    public async Task DoesNothingIfTheUserDoesNotConfirm()
    {
        SelectRadioheadAndOneBjorkAlbum();
        _console.Input.PushKey(ConsoleKey.DownArrow);
        _console.Input.PushKey(ConsoleKey.Enter);
        _console.Input.PushTextWithEnter("n");

        var exitCode = await RunAsync();

        Assert.Equal(1, exitCode);
        Assert.Empty(_library.RemovedAlbums);
        Assert.Empty(_library.UnfollowedArtists);
    }

    [Fact]
    public async Task DoesNothingIfTheUserCancelsTheSelection()
    {
        _console.Input.PushKey(ConsoleKey.Spacebar);
        _console.Input.PushKey(ConsoleKey.Escape);

        var exitCode = await RunAsync();

        Assert.Equal(1, exitCode);
        Assert.Empty(_library.RemovedAlbums);
    }

    private sealed class NoRemainingArguments : IRemainingArguments
    {
        public ILookup<string, string?> Parsed { get; } = Array.Empty<string>().ToLookup(x => x, string? (x) => x);

        public IReadOnlyList<string> Raw { get; } = [];
    }

    private sealed class FakeSpotifyLibrary(IReadOnlyList<Album> albums, IReadOnlyList<Artist> followed)
        : ISpotifyLibrary
    {
        public List<Album> RemovedAlbums { get; } = [];

        public List<Artist> UnfollowedArtists { get; } = [];

        public Task<IReadOnlyList<Album>> GetSavedAlbumsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(albums);

        public Task<IReadOnlyList<Artist>> GetFollowedArtistsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(followed);

        public Task<IReadOnlyList<Artist>> FilterFollowedAsync(
            IReadOnlyCollection<Artist> artists,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Artist>>(artists.Where(followed.Contains).ToList());

        public Task RemoveAlbumsAsync(
            IReadOnlyCollection<Album> albumsToRemove,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            RemovedAlbums.AddRange(albumsToRemove);
            return Task.CompletedTask;
        }

        public Task UnfollowArtistsAsync(
            IReadOnlyCollection<Artist> artists,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            UnfollowedArtists.AddRange(artists);
            return Task.CompletedTask;
        }
    }
}
