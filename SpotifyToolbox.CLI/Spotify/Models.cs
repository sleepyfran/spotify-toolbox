namespace SpotifyToolbox.CLI.Spotify;

public sealed record Artist(string Id, string Name)
{
    public string Uri => $"spotify:artist:{Id}";
}

public sealed record Album(string Id, string Name, string ReleaseDate, IReadOnlyList<Artist> Artists)
{
    public string Uri => $"spotify:album:{Id}";

    /// <summary>
    /// Main artist of the album, which is the one Spotify lists first.
    /// </summary>
    public Artist MainArtist => Artists[0];

    public string ReleaseYear => ReleaseDate.Length >= 4 ? ReleaseDate[..4] : ReleaseDate;
}
