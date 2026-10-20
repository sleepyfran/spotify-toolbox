using SpotifyToolbox.CLI.Spotify;

namespace SpotifyToolbox.Tests;

internal static class TestData
{
    public static readonly Artist Radiohead = new("radiohead", "Radiohead");
    public static readonly Artist Bjork = new("bjork", "Björk");
    public static readonly Artist Thom = new("thom", "Thom Yorke");
    public static readonly Artist Beck = new("beck", "Beck");

    public static readonly Album OkComputer = new("ok-computer", "OK Computer", "1997-05-21", [Radiohead]);
    public static readonly Album KidA = new("kid-a", "Kid A", "2000-10-02", [Radiohead]);
    public static readonly Album TheBends = new("the-bends", "The Bends", "1995-03-13", [Radiohead]);
    public static readonly Album Homogenic = new("homogenic", "Homogenic", "1997-09-22", [Bjork]);
    public static readonly Album Eraser = new("eraser", "The Eraser", "2006", [Thom]);
    public static readonly Album Collaboration = new("collab", "Collaboration", "2010", [Bjork, Thom]);
    public static readonly Album OddelayBeck = new("odelay", "Odelay", "1996-06-18", [Beck]);

    public static IReadOnlyList<Album> Library =>
        [OkComputer, KidA, TheBends, Homogenic, Eraser, Collaboration, OddelayBeck];
}
