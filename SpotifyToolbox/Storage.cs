using Newtonsoft.Json;

namespace SpotifyToolbox.CLI;

public static class Storage
{
    private const string AuthConfigPath = "auth_config.json";

    public static void Save(AuthConfig authConfig)
    {
        File.WriteAllText(AuthConfigPath, JsonConvert.SerializeObject(authConfig));
    }

    public static async Task<AuthConfig?> ReadAuthConfig()
    {
        try
        {
            var configContent = await File.ReadAllTextAsync(AuthConfigPath);
            return JsonConvert.DeserializeObject<AuthConfig>(configContent);
        }
        catch
        {
            return null;
        }
    }
}