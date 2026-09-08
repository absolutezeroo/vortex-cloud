using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

/// <summary>
/// Send a placed item back to its owner's hand.
/// </summary>
/// <param name="RoomId">The room holding it. The room grain owns a placed item, so the pickup goes
/// through it rather than around it -- everyone standing there sees the item leave.</param>
public sealed record PickUpFurnitureRequest(int RoomId, int ItemId, string Reason)
    : IReasonedRequest;
