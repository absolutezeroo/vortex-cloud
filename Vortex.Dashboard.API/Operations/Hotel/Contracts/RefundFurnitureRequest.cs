using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

/// <summary>Take one item back and pay the player what the catalogue asks for it today.</summary>
public sealed record RefundFurnitureRequest(int PlayerId, int ItemId, string Reason)
    : IReasonedRequest;
