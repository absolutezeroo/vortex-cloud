using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Users.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class ExtendedProfileMessageComposerSerializer(int header)
    : AbstractSerializer<ExtendedProfileMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ExtendedProfileMessageComposer message)
    {
        packet
            .WriteInteger(message.UserId)
            .WriteString(message.UserName)
            .WriteString(message.Figure)
            .WriteString(message.Motto)
            .WriteString(message.CreationDate)
            .WriteInteger(message.AchievementScore)
            .WriteInteger(message.FriendCount)
            .WriteBoolean(message.IsFriend)
            .WriteBoolean(message.IsFriendRequestSent)
            .WriteBoolean(message.IsOnline)
            .WriteInteger(message.Guilds.Count);

        foreach (GuildInfoSnapshot guild in message.Guilds)
        {
            GuildInfoSerializer.Serialize(packet, guild);
        }

        packet
            .WriteInteger(message.LastAccessSinceInSeconds)
            .WriteBoolean(message.OpenProfileWindow)
            .WriteBoolean(message.IsHidden)
            .WriteInteger(message.AccountLevel)
            .WriteInteger(message.IntegerField24)
            .WriteInteger(message.StarGemCount)
            .WriteBoolean(message.BooleanField26)
            .WriteBoolean(message.BooleanField27);
    }
}
