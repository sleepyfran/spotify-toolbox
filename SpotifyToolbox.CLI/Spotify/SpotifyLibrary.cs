using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;

namespace SpotifyToolbox.CLI.Spotify;

/// <summary>
/// Thin layer on top of the Spotify client that exposes the library operations the toolbox needs,
/// taking care of pagination and batching.
/// </summary>
public interface ISpotifyLibrary
{
    Task<IReadOnlyList<Album>> GetSavedAlbumsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Artist>> GetFollowedArtistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns which of the given artists are followed by the current user.
    /// </summary>
    Task<IReadOnlyList<Artist>> FilterFollowedAsync(
        IReadOnlyCollection<Artist> artists,
        CancellationToken cancellationToken = default);

    Task RemoveAlbumsAsync(
        IReadOnlyCollection<Album> albums,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    Task UnfollowArtistsAsync(
        IReadOnlyCollection<Artist> artists,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <remarks>
/// Removing items and checking whether they're saved/followed goes through the unified
/// <c>/me/library</c> endpoints, which since February 2026 replace the type-specific ones (which
/// are no longer available to apps in development mode). Removals go straight through the API
/// connector because SpotifyAPI.Web sends the URIs in the request body, which Spotify ignores
/// and fails with "Missing required field: uris" since it only reads them from the query string.
/// </remarks>
public sealed class SpotifyLibrary(ISpotifyClient client, IAPIConnector api) : ISpotifyLibrary
{
    /// <summary>
    /// Maximum amount of URIs that Spotify accepts in a single library request. SpotifyAPI.Web
    /// documents 50, but the API rejects anything above 40 with "Too many uris requested".
    /// </summary>
    private const int BatchSize = 40;

    public async Task<IReadOnlyList<Album>> GetSavedAlbumsAsync(CancellationToken cancellationToken = default)
    {
        var firstPage = await client.Library.GetAlbums(new LibraryAlbumsRequest(), cancellationToken);
        var savedAlbums = await client.PaginateAll(firstPage, null, cancellationToken);

        return savedAlbums
            .Select(saved => saved.Album)
            .Select(album => new Album(
                album.Id,
                album.Name,
                album.ReleaseDate ?? string.Empty,
                album.Artists.Count > 0
                    ? album.Artists.Select(artist => new Artist(artist.Id, artist.Name)).ToList()
                    : [new Artist(string.Empty, "Unknown artist")]))
            .ToList();
    }

    public async Task<IReadOnlyList<Artist>> GetFollowedArtistsAsync(CancellationToken cancellationToken = default)
    {
        var firstPage = await client.Follow.OfCurrentUser(new FollowOfCurrentUserRequest(), cancellationToken);
        var artists = await client.PaginateAll(
            firstPage.Artists,
            response => response.Artists,
            null,
            cancellationToken);

        return artists.Select(artist => new Artist(artist.Id, artist.Name)).ToList();
    }

    public async Task<IReadOnlyList<Artist>> FilterFollowedAsync(
        IReadOnlyCollection<Artist> artists,
        CancellationToken cancellationToken = default)
    {
        var followed = new List<Artist>();

        foreach (var batch in artists.Where(artist => artist.Id.Length > 0).Chunk(BatchSize))
        {
            var results = await client.Library.CheckItems(
                new LibraryCheckItemsRequest(batch.Select(artist => artist.Uri).ToList()),
                cancellationToken);

            followed.AddRange(batch.Where((_, index) => index < results.Count && results[index]));
        }

        return followed;
    }

    public Task RemoveAlbumsAsync(
        IReadOnlyCollection<Album> albums,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default) =>
        RemoveFromLibraryAsync(albums.Select(album => album.Uri).ToList(), progress, cancellationToken);

    public Task UnfollowArtistsAsync(
        IReadOnlyCollection<Artist> artists,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default) =>
        RemoveFromLibraryAsync(artists.Select(artist => artist.Uri).ToList(), progress, cancellationToken);

    private async Task RemoveFromLibraryAsync(
        IReadOnlyList<string> uris,
        IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        var processed = 0;

        foreach (var batch in uris.Chunk(BatchSize))
        {
            await api.Delete(
                SpotifyUrls.Library(),
                new Dictionary<string, string> { ["uris"] = string.Join(',', batch) },
                null,
                cancellationToken);

            processed += batch.Length;
            progress?.Report(processed);
        }
    }
}
