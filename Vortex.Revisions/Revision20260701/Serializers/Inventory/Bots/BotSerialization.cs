using Vortex.Primitives.Bots;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Bots;

/// <summary>
/// Shared wire writer for the WIN63 bot block, decoded from the client's class_3143 constructor.
/// Note the order: it reads gender BEFORE figure, while its own getters list figure first — writing
/// them the way the getters read would swap a bot's look and sex on every row.
/// </summary>
internal static class BotSerialization
{
    public static void WriteBot(IServerPacket packet, BotSnapshot bot) =>
        packet
            .WriteInteger(bot.BotId)
            .WriteString(bot.Name)
            .WriteString(bot.Motto)
            .WriteString(bot.Gender == AvatarGenderType.Female ? "f" : "m")
            .WriteString(bot.Figure);
}
