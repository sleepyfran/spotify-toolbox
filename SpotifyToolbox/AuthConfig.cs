using SpotifyAPI.Web;

namespace SpotifyToolbox.CLI;

public class AuthConfig
{
    public PKCETokenResponse Token { get; set; }

    public AuthConfig(PKCETokenResponse token)
    {
        Token = token;
    }
}

public class AppConfig
{
    public string ClientId { get; set; }
    public AuthConfig? Auth { get; set; }

    public AppConfig(string clientId, AuthConfig? auth = null)
    {
        Auth = auth;
        ClientId = clientId;
    }
}