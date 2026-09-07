namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One hand item.
/// </summary>
/// <param name="HandItemId">The client's id, which is what furniture and pets refer to; <c>Id</c>
/// is only this table's own key.</param>
/// <param name="Consumable">Whether eating it does anything, i.e. either figure below is non-zero.
/// False is correct for a camera and a bug for a plate of food.</param>
/// <param name="ImageUrl">An avatar holding the item: the client has no icon for one anywhere.</param>
public sealed record HandItemRow(
    int Id,
    int HandItemId,
    string Name,
    int Nutrition,
    int Thirst,
    bool Consumable,
    string? ImageUrl
);
