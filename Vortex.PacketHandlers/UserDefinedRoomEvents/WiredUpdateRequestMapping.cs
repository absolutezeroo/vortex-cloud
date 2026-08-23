using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents;

/// <summary>
/// The one place the six wired <c>Update*Message</c> packets become the request the room engine
/// takes. Shared rather than repeated in each handler: they differ only in which message type the
/// registry routed, never in what the engine is told.
/// </summary>
internal static class WiredUpdateRequestMapping
{
    public static WiredUpdateRequest ToRequest(this UpdateWiredMessage message) =>
        new()
        {
            Id = message.Id,
            IntParams = message.IntParams,
            StringParam = message.StringParam,
            StuffIds = message.StuffIds,
            StuffIds2 = message.StuffIds2,
            DefinitionSpecifics = message.DefinitionSpecifics,
            FurniSources = message.FurniSources,
            PlayerSources = message.PlayerSources,
            VariableIds = message.VariableIds,
            TypeSpecifics = message.TypeSpecifics,
        };
}
