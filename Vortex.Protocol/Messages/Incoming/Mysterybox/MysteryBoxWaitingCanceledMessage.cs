using Vortex.Primitives.Networking;
using Vortex.Primitives.Players;

namespace Vortex.Protocol.Messages.Incoming.Mysterybox;

/// <summary>
/// Sent when the player closes/cancels the mystery box wait dialog. The client passes the box
/// furniture's owner id (<c>getFurnitureOwnerId(object)</c>), which is what identifies the pending
/// open session inside the room -- not the furniture id.
/// </summary>
public record MysteryBoxWaitingCanceledMessage : IMessageEvent
{
    public required PlayerId BoxOwnerId { get; init; }
}
