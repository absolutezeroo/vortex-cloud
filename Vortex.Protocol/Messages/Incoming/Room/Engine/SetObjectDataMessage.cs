using System.Collections.Immutable;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Engine;

/// <summary>
/// Writes a furni's key/value data — the generic form behind the toner, the badge display and the
/// other furniture whose editor is a set of named fields rather than a single note.
/// </summary>
public record SetObjectDataMessage : IMessageEvent
{
    public required int ItemId { get; init; }

    public required ImmutableArray<(string Key, string Value)> Pairs { get; init; }
}
