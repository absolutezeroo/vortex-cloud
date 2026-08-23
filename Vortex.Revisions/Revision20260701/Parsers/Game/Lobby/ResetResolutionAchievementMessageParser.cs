using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Game.Lobby;

namespace Vortex.Revisions.Revision20260701.Parsers.Game.Lobby;

internal class ResetResolutionAchievementMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ResetResolutionAchievementMessage { StuffId = packet.PopInt() };
}
