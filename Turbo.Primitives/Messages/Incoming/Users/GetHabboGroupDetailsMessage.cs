using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Users;

public record GetHabboGroupDetailsMessage : IMessageEvent
{
    public required int GroupId { get; init; }

    /// <summary>Client-set flag (request for the full detail view); kept for protocol fidelity.</summary>
    public required bool RequestDetails { get; init; }
}
