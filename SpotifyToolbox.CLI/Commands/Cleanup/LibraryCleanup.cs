using SpotifyToolbox.CLI.Prompts;
using SpotifyToolbox.CLI.Spotify;

namespace SpotifyToolbox.CLI.Commands.Cleanup;

/// <summary>
/// Pure logic behind the cleanup command, kept apart from the console and the Spotify API.
/// </summary>
public static class LibraryCleanup
{
    /// <summary>
    /// Groups the albums by their main artist, sorting artists by name and their albums by
    /// release date.
    /// </summary>
    public static IReadOnlyList<TreeGroup<Artist, Album>> GroupByArtist(IEnumerable<Album> albums) =>
        albums
            .GroupBy(album => album.MainArtist.Id.Length > 0 ? album.MainArtist.Id : album.MainArtist.Name)
            .Select(group => new TreeGroup<Artist, Album>(
                group.First().MainArtist,
                group
                    .OrderBy(album => album.ReleaseDate, StringComparer.Ordinal)
                    .ThenBy(album => album.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()))
            .OrderBy(group => group.Value.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>
    /// Returns the artists that appear in the removed albums but in none of the albums that will
    /// remain in the library, considering every artist credited in an album and not only the main
    /// one.
    /// </summary>
    public static IReadOnlyList<Artist> FindArtistsWithoutAlbums(
        IEnumerable<Album> library,
        IReadOnlyCollection<Album> removedAlbums)
    {
        var removedIds = removedAlbums.Select(album => album.Id).ToHashSet();
        var remainingArtistIds = library
            .Where(album => !removedIds.Contains(album.Id))
            .SelectMany(album => album.Artists)
            .Select(artist => artist.Id)
            .ToHashSet();

        return removedAlbums
            .SelectMany(album => album.Artists)
            .Where(artist => artist.Id.Length > 0 && !remainingArtistIds.Contains(artist.Id))
            .DistinctBy(artist => artist.Id)
            .OrderBy(artist => artist.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}
