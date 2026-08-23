using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Protocol.Messages.Incoming.Room.Avatar;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Avatar;

internal class ChangePostureMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ChangePostureMessage { PostureType = (AvatarPostureType)packet.PopInt() };
}
