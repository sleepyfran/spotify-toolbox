using SpotifyToolbox.CLI.Prompts;

namespace SpotifyToolbox.Tests;

public class TreeGroupTests
{
    private static TreeGroup<string, string> CreateGroup() => new("artist", ["a", "b", "c"]);

    [Fact]
    public void StartsWithNothingSelected()
    {
        var group = CreateGroup();

        Assert.Equal(SelectionState.None, group.State);
        Assert.Empty(group.SelectedItems);
    }

    [Fact]
    public void SelectingSomeItemsMakesTheGroupPartiallySelected()
    {
        var group = CreateGroup();

        group.Toggle(1);

        Assert.Equal(SelectionState.Partial, group.State);
        Assert.Equal(["b"], group.SelectedItems);
    }

    [Fact]
    public void SelectingAllItemsMakesTheGroupSelected()
    {
        var group = CreateGroup();

        group.Toggle(0);
        group.Toggle(1);
        group.Toggle(2);

        Assert.Equal(SelectionState.All, group.State);
    }

    [Fact]
    public void ToggleAllSelectsEverythingWhenPartiallySelected()
    {
        var group = CreateGroup();
        group.Toggle(0);

        group.ToggleAll();

        Assert.Equal(SelectionState.All, group.State);
        Assert.Equal(["a", "b", "c"], group.SelectedItems);
    }

    [Fact]
    public void ToggleAllClearsTheSelectionWhenEverythingIsSelected()
    {
        var group = CreateGroup();
        group.ToggleAll();

        group.ToggleAll();

        Assert.Equal(SelectionState.None, group.State);
    }

    [Fact]
    public void RequiresAtLeastOneItem()
    {
        Assert.Throws<ArgumentException>(() => new TreeGroup<string, string>("artist", []));
    }
}
