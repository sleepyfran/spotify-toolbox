using SpotifyAPI.Web;

namespace SpotifyToolbox.CLI;

public abstract class Context
{
    public string ClientId { get; set; }
}

public class UnauthenticatedContext : Context
{
    public UnauthenticatedContext(string clientId)
    {
        ClientId = clientId;
    }
}

public class AuthenticatedContext : Context
{
    public AuthConfig AuthConfig;
    public SpotifyClient Client;

    public AuthenticatedContext(string clientId, AuthConfig authConfig, SpotifyClient client)
    {
        ClientId = clientId;
        AuthConfig = authConfig;
        Client = client;
    }
}