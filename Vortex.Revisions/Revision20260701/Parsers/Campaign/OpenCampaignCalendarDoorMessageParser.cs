using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Campaign;

namespace Vortex.Revisions.Revision20260701.Parsers.Campaign;

internal class OpenCampaignCalendarDoorMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new OpenCampaignCalendarDoorMessage();
}
