using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Messages.Outgoing.Handshake;

public sealed record GenericErrorMessage : IComposer
{
    public required RoomGenericErrorType ErrorCode { get; init; }
}
