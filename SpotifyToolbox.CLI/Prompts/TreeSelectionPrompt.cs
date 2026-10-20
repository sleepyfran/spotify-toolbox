using Spectre.Console;
using Spectre.Console.Rendering;

namespace SpotifyToolbox.CLI.Prompts;

/// <summary>
/// Multi-selection prompt that shows a list of collapsible groups. Groups can be selected as a
/// whole or expanded (right arrow) to select individual items, and collapsed again (left arrow)
/// keeping the selection, in which case the group is shown as partially selected.
/// </summary>
public sealed class TreeSelectionPrompt<TGroup, TItem>(
    IReadOnlyList<TreeGroup<TGroup, TItem>> groups,
    Func<TGroup, string> groupConverter,
    Func<TItem, string> itemConverter)
{
    private readonly record struct Row(int GroupIndex, int ItemIndex)
    {
        public bool IsGroup => ItemIndex < 0;
    }

    private enum KeyResult
    {
        Continue,
        Submit,
        Cancel
    }

    private List<Row> _rows = [];
    private int _cursor;
    private int _scrollOffset;
    private int _lastPageSize = 10;

    public string Title { get; init; } = "Select the items";

    /// <summary>
    /// Name used for the items and groups in the selection summary, e.g. "album(s)".
    /// </summary>
    public string ItemsName { get; init; } = "item(s)";

    public string GroupsName { get; init; } = "group(s)";

    /// <summary>
    /// Amount of rows to display at once. Defaults to whatever fits in the console.
    /// </summary>
    public int? PageSize { get; init; }

    /// <summary>
    /// Shows the prompt and returns the groups with their selection, or null if the user cancelled.
    /// </summary>
    public async Task<IReadOnlyList<TreeGroup<TGroup, TItem>>?> ShowAsync(
        IAnsiConsole console,
        CancellationToken cancellationToken = default)
    {
        if (!console.Profile.Capabilities.Interactive)
        {
            throw new InvalidOperationException(
                "Cannot show the selection prompt since the current terminal isn't interactive");
        }

        if (groups.Count == 0)
        {
            return groups;
        }

        RebuildRows();

        var result = KeyResult.Continue;
        console.Cursor.Hide();
        try
        {
            await console
                .Live(Render(GetPageSize(console)))
                .AutoClear(true)
                .Overflow(VerticalOverflow.Crop)
                .StartAsync(async context =>
                {
                    while (result == KeyResult.Continue)
                    {
                        var key = await console.Input.ReadKeyAsync(true, cancellationToken);
                        if (key == null)
                        {
                            continue;
                        }

                        result = HandleKey(key.Value);
                        context.UpdateTarget(Render(GetPageSize(console)));
                        context.Refresh();
                    }
                });
        }
        finally
        {
            console.Cursor.Show();
        }

        return result == KeyResult.Submit ? groups : null;
    }

    private int GetPageSize(IAnsiConsole console) =>
        PageSize ?? Math.Max(5, console.Profile.Height - 6);

    private KeyResult HandleKey(ConsoleKeyInfo key)
    {
        var row = _rows[_cursor];
        var group = groups[row.GroupIndex];

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                MoveCursor(-1);
                break;
            case ConsoleKey.DownArrow:
                MoveCursor(1);
                break;
            case ConsoleKey.PageUp:
                MoveCursor(-_lastPageSize);
                break;
            case ConsoleKey.PageDown:
                MoveCursor(_lastPageSize);
                break;
            case ConsoleKey.Home:
                _cursor = 0;
                break;
            case ConsoleKey.End:
                _cursor = _rows.Count - 1;
                break;
            case ConsoleKey.RightArrow when row.IsGroup:
                group.IsExpanded = true;
                RebuildRows();
                _cursor = _rows.IndexOf(new Row(row.GroupIndex, 0));
                break;
            case ConsoleKey.LeftArrow:
                group.IsExpanded = false;
                RebuildRows();
                _cursor = _rows.IndexOf(new Row(row.GroupIndex, -1));
                break;
            case ConsoleKey.Spacebar:
                if (row.IsGroup)
                {
                    group.ToggleAll();
                }
                else
                {
                    group.Toggle(row.ItemIndex);
                }

                break;
            case ConsoleKey.Enter:
                return KeyResult.Submit;
            case ConsoleKey.Escape:
                return KeyResult.Cancel;
            default:
                if (char.IsLetterOrDigit(key.KeyChar))
                {
                    JumpToGroupStartingWith(key.KeyChar);
                }

                break;
        }

        return KeyResult.Continue;
    }

    private void MoveCursor(int delta)
    {
        _cursor = Math.Clamp(_cursor + delta, 0, _rows.Count - 1);
    }

    /// <summary>
    /// Moves the cursor to the next group (after the current one, wrapping around) whose name
    /// starts with the given character, which makes it easy to navigate big libraries.
    /// </summary>
    private void JumpToGroupStartingWith(char character)
    {
        var currentGroup = _rows[_cursor].GroupIndex;
        for (var offset = 1; offset <= groups.Count; offset++)
        {
            var groupIndex = (currentGroup + offset) % groups.Count;
            var name = groupConverter(groups[groupIndex].Value);
            if (name.StartsWith(character.ToString(), StringComparison.CurrentCultureIgnoreCase))
            {
                _cursor = _rows.IndexOf(new Row(groupIndex, -1));
                return;
            }
        }
    }

    private void RebuildRows()
    {
        _rows = [];
        for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            var group = groups[groupIndex];
            _rows.Add(new Row(groupIndex, -1));

            if (!group.IsExpanded)
            {
                continue;
            }

            for (var itemIndex = 0; itemIndex < group.Items.Count; itemIndex++)
            {
                _rows.Add(new Row(groupIndex, itemIndex));
            }
        }
    }

    private IRenderable Render(int pageSize)
    {
        _lastPageSize = pageSize;

        // Keep the cursor within the visible window.
        if (_cursor < _scrollOffset)
        {
            _scrollOffset = _cursor;
        }
        else if (_cursor >= _scrollOffset + pageSize)
        {
            _scrollOffset = _cursor - pageSize + 1;
        }

        _scrollOffset = Math.Clamp(_scrollOffset, 0, Math.Max(0, _rows.Count - pageSize));

        var lines = new List<IRenderable>
        {
            new Markup($"[bold]{Markup.Escape(Title)}[/]"),
            new Markup(
                "[grey](↑/↓ move, [blue]space[/] toggle, [blue]→[/] expand, [blue]←[/] collapse, " +
                "[blue]enter[/] confirm, [blue]esc[/] cancel)[/]"),
        };

        lines.AddRange(
            _rows
                .Skip(_scrollOffset)
                .Take(pageSize)
                .Select((row, index) => RenderRow(row, _scrollOffset + index == _cursor)));

        if (_rows.Count > pageSize)
        {
            lines.Add(new Markup(
                $"[grey]Showing {_scrollOffset + 1}-{Math.Min(_scrollOffset + pageSize, _rows.Count)} of {_rows.Count}[/]"));
        }

        var selectedItems = groups.Sum(group => group.SelectedCount);
        var selectedGroups = groups.Count(group => group.SelectedCount > 0);
        lines.Add(new Markup(
            $"[yellow]Selected: {selectedItems} {Markup.Escape(ItemsName)} from {selectedGroups} {Markup.Escape(GroupsName)}[/]"));

        return new Rows(lines);
    }

    private Markup RenderRow(Row row, bool isCurrent)
    {
        var group = groups[row.GroupIndex];

        string content;
        if (row.IsGroup)
        {
            var expander = group.IsExpanded ? "▾" : "▸";
            var count = group.State == SelectionState.Partial
                ? $"{group.SelectedCount}/{group.Items.Count}"
                : group.Items.Count.ToString();
            content =
                $"{expander} {Checkbox(group.State)} {Markup.Escape(groupConverter(group.Value))} [grey]({count})[/]";
        }
        else
        {
            var state = group.IsSelected(row.ItemIndex) ? SelectionState.All : SelectionState.None;
            content = $"    {Checkbox(state)} {Markup.Escape(itemConverter(group.Items[row.ItemIndex]))}";
        }

        var pointer = isCurrent ? "[blue]>[/]" : " ";
        var line = $"{pointer} {content}";
        return new Markup(isCurrent ? $"[bold]{line}[/]" : line);
    }

    private static string Checkbox(SelectionState state) => state switch
    {
        SelectionState.All => "[green][[X]][/]",
        SelectionState.Partial => "[yellow][[-]][/]",
        _ => "[[ ]]"
    };
}
