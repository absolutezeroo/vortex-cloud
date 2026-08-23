using Vortex.Protocol.Messages.Incoming.Game.Lobby;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Game.Lobby;

internal class GetResolutionAchievementsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetResolutionAchievementsMessage
        {
            StuffId = packet.PopInt(),
            AchievementId = packet.PopInt(),
        };
}
