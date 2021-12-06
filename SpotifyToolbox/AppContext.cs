using SpotifyAPI.Web;

namespace SpotifyToolbox.CLI;

public abstract class AppContext
{
    public string ClientId { get; set; }
}

public class UnauthenticatedAppContext : AppContext
{
    public UnauthenticatedAppContext(string clientId)
    {
        ClientId = clientId;
    }
}

public class AuthenticatedAppContext : AppContext
{
    public AuthConfig AuthConfig;
    public SpotifyClient Client;

    public AuthenticatedAppContext(string clientId, AuthConfig authConfig, SpotifyClient client)
    {
        ClientId = clientId;
        AuthConfig = authConfig;
        Client = client;
    }
}