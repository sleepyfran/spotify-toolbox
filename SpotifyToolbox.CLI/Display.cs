using Spectre.Console;
using SpotifyToolbox.CLI.Spotify;

namespace SpotifyToolbox.CLI;

/// <summary>
/// Shared helpers to display library items and long running operations in the console.
/// </summary>
public static class Display
{
    public static string Artist(Artist artist) => $"[bold]{Markup.Escape(artist.Name)}[/]";

    public static string Album(Album album) =>
        $"[bold]{Markup.Escape(album.Name)}[/] by {Artist(album.MainArtist)}";

    /// <summary>
    /// Runs the given operation showing a progress bar that the operation updates by reporting
    /// how many items it has processed so far.
    /// </summary>
    public static Task RunWithProgressAsync(
        this IAnsiConsole console,
        string description,
        int total,
        Func<IProgress<int>, Task> operation) =>
        console
            .Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async context =>
            {
                var task = context.AddTask(Markup.Escape(description), maxValue: total);
                await operation(new SynchronousProgress(processed => task.Value = processed));
                task.Value = total;
            });

    /// <summary>
    /// Unlike <see cref="Progress{T}"/> this reports progress immediately in the calling thread,
    /// which avoids updates arriving out of order or after the operation has finished.
    /// </summary>
    private sealed class SynchronousProgress(Action<int> report) : IProgress<int>
    {
        public void Report(int value) => report(value);
    }
}
