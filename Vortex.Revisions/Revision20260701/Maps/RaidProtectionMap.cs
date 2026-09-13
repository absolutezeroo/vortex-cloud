using Vortex.Primitives.Networking.Revisions;
using Vortex.Protocol.Messages.Outgoing.Room.RaidProtection;
using Vortex.Revisions.Revision20260701.Parsers.Room.RaidProtection;
using Vortex.Revisions.Revision20260701.Serializers.Room.RaidProtection;

namespace Vortex.Revisions.Revision20260701.Maps;

/// <summary>
/// The room raid-protection panel: two things the owner asks for, three the room answers with.
/// </summary>
/// <remarks>
/// Split out of <see cref="RoomSettingsMap" /> on purpose. The two halves sit on opposite sides of
/// the id story — the parsers are on the client's own AIR ids, the serializers had to move into the
/// Vortex band — and mixing them into the room-settings map would bury that where nobody reads it.
/// </remarks>
internal sealed class RaidProtectionMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.GetRaidProtectionSettingsMessageEvent,
            new GetRaidProtectionSettingsMessageParser()
        );
        builder.MapParser(
            MessageEvent.SaveRaidProtectionSettingsMessageEvent,
            new SaveRaidProtectionSettingsMessageParser()
        );

        builder.MapSerializer(
            typeof(RaidProtectionCapabilityMessageComposer),
            new RaidProtectionCapabilityMessageComposerSerializer(
                MessageComposer.RaidProtectionCapabilityMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(RaidProtectionSettingsMessageComposer),
            new RaidProtectionSettingsMessageComposerSerializer(
                MessageComposer.RaidProtectionSettingsMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(RaidProtectionSettingsResultMessageComposer),
            new RaidProtectionSettingsResultMessageComposerSerializer(
                MessageComposer.RaidProtectionSettingsResultMessageComposer
            )
        );
    }
}
