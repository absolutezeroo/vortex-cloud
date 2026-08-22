using System.Collections.Immutable;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>The player putting furniture on, or taking it off, a wired trade's table.</summary>
/// <remarks>
/// One message serves both directions — <see cref="Remove"/> says which — and the client sends a
/// single id when removing and a whole group when adding, which is why this is a list either way.
/// </remarks>
public record WiredTradeUpdateItemsMessage : IMessageEvent
{
    public required bool Remove { get; init; }

    public required ImmutableArray<int> ItemIds { get; init; }
}
