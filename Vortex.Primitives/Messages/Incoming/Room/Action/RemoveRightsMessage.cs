using System.Collections.Immutable;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Action;

public record RemoveRightsMessage : IMessageEvent
{
    public required ImmutableArray<int> TargetUserIds { get; init; }
}
