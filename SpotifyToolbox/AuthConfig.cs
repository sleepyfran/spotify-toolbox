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