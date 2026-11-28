namespace Vibe.UI.Components;

/// <summary>
/// What a <see cref="VibeGrid{TGridItem}"/> asks its provider for: one page, in one order.
/// It names nothing from any server contract, so an app maps it onto its own paged query.
/// </summary>
/// <param name="Page">The page wanted, counted from zero.</param>
/// <param name="PageSize">How many items a page holds.</param>
/// <param name="SortBy">The name of the property to sort by, or null when the grid is unsorted.</param>
/// <param name="SortDescending">Whether the sort runs from high to low.</param>
/// <param name="CancellationToken">Cancelled when the grid no longer wants the answer.</param>
public readonly record struct VibeGridRequest(
    int Page,
    int PageSize,
    string? SortBy,
    bool SortDescending,
    CancellationToken CancellationToken
);

/// <summary>One page of items and how many items there are across all pages.</summary>
/// <typeparam name="TItem">The type of a row.</typeparam>
/// <param name="Items">The items on this page.</param>
/// <param name="TotalCount">The number of items on every page together.</param>
public sealed record VibeGridPage<TItem>(IReadOnlyCollection<TItem> Items, int TotalCount);
