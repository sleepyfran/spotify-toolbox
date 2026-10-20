using SpotifyToolbox.CLI.Commands.Cleanup;
using static SpotifyToolbox.Tests.TestData;

namespace SpotifyToolbox.Tests;

public class LibraryCleanupTests
{
    [Fact]
    public void GroupsAlbumsByMainArtistSortedByName()
    {
        var groups = LibraryCleanup.GroupByArtist(Library);

        Assert.Equal(
            [Beck, Bjork, Radiohead, Thom],
            groups.Select(group => group.Value));
        Assert.Equal([Homogenic, Collaboration], groups[1].Items);
    }

    [Fact]
    public void SortsAlbumsOfAnArtistByReleaseDate()
    {
        var radiohead = LibraryCleanup.GroupByArtist(Library).Single(group => group.Value == Radiohead);

        Assert.Equal([TheBends, OkComputer, KidA], radiohead.Items);
    }

    [Fact]
    public void ArtistWithAllAlbumsRemovedHasNoAlbumsLeft()
    {
        var artists = LibraryCleanup.FindArtistsWithoutAlbums(Library, [OkComputer, KidA, TheBends]);

        Assert.Equal([Radiohead], artists);
    }

    [Fact]
    public void ArtistWithSomeAlbumsLeftIsNotReturned()
    {
        var artists = LibraryCleanup.FindArtistsWithoutAlbums(Library, [OkComputer, KidA]);

        Assert.Empty(artists);
    }

    [Fact]
    public void ArtistCreditedInARemainingAlbumIsNotReturned()
    {
        // Thom Yorke still appears in the collaboration with Björk.
        var artists = LibraryCleanup.FindArtistsWithoutAlbums(Library, [Eraser]);

        Assert.Empty(artists);
    }

    [Fact]
    public void ConsidersEveryArtistCreditedInRemovedAlbums()
    {
        var artists = LibraryCleanup.FindArtistsWithoutAlbums(Library, [Homogenic, Collaboration, Eraser]);

        Assert.Equal([Bjork, Thom], artists);
    }
}
