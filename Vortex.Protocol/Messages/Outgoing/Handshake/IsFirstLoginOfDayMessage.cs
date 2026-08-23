using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Handshake;

public sealed record IsFirstLoginOfDayMessage : IComposer
{
    public required bool IsFirstLoginOfDay { get; init; }
}
