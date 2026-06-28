using System.Collections.Immutable;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Room.Action;

public record RemoveRightsMessage : IMessageEvent
{
    public required ImmutableArray<int> TargetUserIds { get; init; }
}
