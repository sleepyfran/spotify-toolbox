namespace SpotifyToolbox.CLI.Prompts;

public enum SelectionState
{
    None,
    Partial,
    All
}

/// <summary>
/// Group of selectable items (e.g. an artist and its albums) shown as a single collapsible node
/// in a <see cref="TreeSelectionPrompt{TGroup,TItem}"/>.
/// </summary>
public sealed class TreeGroup<TGroup, TItem>
{
    private readonly bool[] _selected;

    public TreeGroup(TGroup value, IReadOnlyList<TItem> items)
    {
        if (items.Count == 0)
        {
            throw new ArgumentException("A group needs at least one item", nameof(items));
        }

        Value = value;
        Items = items;
        _selected = new bool[items.Count];
    }

    public TGroup Value { get; }

    public IReadOnlyList<TItem> Items { get; }

    public bool IsExpanded { get; set; }

    public int SelectedCount => _selected.Count(selected => selected);

    public IEnumerable<TItem> SelectedItems => Items.Where((_, index) => _selected[index]);

    public SelectionState State => SelectedCount switch
    {
        0 => SelectionState.None,
        var count when count == Items.Count => SelectionState.All,
        _ => SelectionState.Partial
    };

    public bool IsSelected(int itemIndex) => _selected[itemIndex];

    public void Toggle(int itemIndex) => _selected[itemIndex] = !_selected[itemIndex];

    /// <summary>
    /// Selects every item unless all of them are already selected, in which case it clears the
    /// selection. This mimics how parent checkboxes usually behave in tree views.
    /// </summary>
    public void ToggleAll()
    {
        var select = State != SelectionState.All;
        Array.Fill(_selected, select);
    }
}
