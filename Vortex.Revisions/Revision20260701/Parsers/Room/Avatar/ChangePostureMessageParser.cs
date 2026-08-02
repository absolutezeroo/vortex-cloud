using Vortex.Primitives.Messages.Incoming.Room.Avatar;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Avatar;

internal class ChangePostureMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ChangePostureMessage { PostureType = (AvatarPostureType)packet.PopInt() };
}
