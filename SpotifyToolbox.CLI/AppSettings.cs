namespace SpotifyToolbox.CLI;

/// <param name="ClientId">Client ID of the Spotify app used to authenticate.</param>
/// <param name="IsLoggedIn">Whether the user has already authenticated with Spotify.</param>
public sealed record AppSettings(string ClientId, bool IsLoggedIn);
