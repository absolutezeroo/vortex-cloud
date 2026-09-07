namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One category a room can be filed under.
/// </summary>
/// <param name="GlobalCategory">The client-side grouping this category belongs to, null when it
/// is not part of one -- it is a name, not a flag.</param>
/// <param name="RoomCount">How many rooms are actually in it -- an empty category is a filter that
/// shows nothing.</param>
public sealed record NavigatorFlatCategoryRow(
    int Id,
    string Name,
    bool Visible,
    bool Automatic,
    string? AutomaticCategory,
    string? GlobalCategory,
    bool StaffOnly,
    int MinRank,
    int OrderNum,
    int RoomCount
);
