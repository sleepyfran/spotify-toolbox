using Newtonsoft.Json;

namespace SpotifyToolbox.CLI.Auth;

/// <summary>
/// Persists the Spotify token between runs. The config lives in the user's config directory
/// (e.g. ~/.config/spotify-toolbox on Linux) so that the CLI works regardless of the current
/// working directory.
/// </summary>
public static class AuthStorage
{
    private const string FileName = "auth_config.json";

    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create),
        "spotify-toolbox"
    );

    private static string ConfigPath => Path.Combine(ConfigDirectory, FileName);

    /// <summary>
    /// Older versions stored the config in the current working directory, which we still read
    /// from so that existing logins are not lost.
    /// </summary>
    private static string LegacyConfigPath => Path.Combine(Environment.CurrentDirectory, FileName);

    public static void Save(AuthConfig authConfig)
    {
        Directory.CreateDirectory(ConfigDirectory);
        File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(authConfig));
    }

    /// <summary>
    /// Deletes the stored config, returning whether there was anything to delete.
    /// </summary>
    public static bool Delete()
    {
        var deleted = false;
        foreach (var path in new[] { ConfigPath, LegacyConfigPath }.Where(File.Exists))
        {
            File.Delete(path);
            deleted = true;
        }

        return deleted;
    }

    public static async Task<AuthConfig?> ReadAsync()
    {
        var config = await TryReadAsync(ConfigPath);
        if (config != null)
        {
            return config;
        }

        config = await TryReadAsync(LegacyConfigPath);
        if (config != null)
        {
            Save(config);
        }

        return config;
    }

    private static async Task<AuthConfig?> TryReadAsync(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var content = await File.ReadAllTextAsync(path);
            var config = JsonConvert.DeserializeObject<AuthConfig>(content);
            return config?.Token is null ? null : config;
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
