namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One room-ad category, with how many ads are live in it.</summary>
public sealed record NavigatorEventCategoryRow(
    int Id,
    string Name,
    bool Visible,
    int ActiveAdCount
);
