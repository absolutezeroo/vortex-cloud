using Vortex.Primitives.Networking.Revisions;
using Vortex.Protocol.Messages.Outgoing.Sound;
using Vortex.Revisions.Revision20260701.Parsers.Sound;
using Vortex.Revisions.Revision20260701.Serializers.Sound;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class SoundMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(MessageEvent.AddJukeboxDiskEvent, new AddJukeboxDiskMessageParser());
        builder.MapParser(
            MessageEvent.GetJukeboxPlayListMessageEvent,
            new GetJukeboxPlayListMessageParser()
        );
        builder.MapParser(MessageEvent.GetNowPlayingMessageEvent, new GetNowPlayingMessageParser());
        builder.MapParser(
            MessageEvent.GetOfficialSongIdMessageEvent,
            new GetOfficialSongIdMessageParser()
        );
        builder.MapParser(MessageEvent.GetSongInfoMessageEvent, new GetSongInfoMessageParser());
        builder.MapParser(
            MessageEvent.GetSoundMachinePlayListMessageEvent,
            new GetSoundMachinePlayListMessageParser()
        );
        builder.MapParser(MessageEvent.GetSoundSettingsEvent, new GetSoundSettingsMessageParser());
        builder.MapParser(
            MessageEvent.GetUserSongDisksMessageEvent,
            new GetUserSongDisksMessageParser()
        );
        builder.MapParser(
            MessageEvent.RemoveJukeboxDiskEvent,
            new RemoveJukeboxDiskMessageParser()
        );

        // The answers. This map registered parsers only, so every sound composer written so far was
        // unreachable: sending one found no serializer and the client heard nothing back.
        builder.MapSerializer(
            typeof(TraxSongInfoMessageComposer),
            new TraxSongInfoMessageComposerSerializer(MessageComposer.TraxSongInfoMessageComposer)
        );
        builder.MapSerializer(
            typeof(OfficialSongIdMessageComposer),
            new OfficialSongIdMessageComposerSerializer(
                MessageComposer.OfficialSongIdMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(UserSongDisksInventoryMessageComposer),
            new UserSongDisksInventoryMessageComposerSerializer(
                MessageComposer.UserSongDisksInventoryMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(JukeboxSongDisksMessageComposer),
            new JukeboxSongDisksMessageComposerSerializer(
                MessageComposer.JukeboxSongDisksMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(JukeboxPlayListFullMessageComposer),
            new JukeboxPlayListFullMessageComposerSerializer(
                MessageComposer.JukeboxPlayListFullMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(NowPlayingMessageComposer),
            new NowPlayingMessageComposerSerializer(MessageComposer.NowPlayingMessageComposer)
        );
        builder.MapSerializer(
            typeof(PlayListMessageComposer),
            new PlayListMessageComposerSerializer(MessageComposer.PlayListMessageComposer)
        );
    }
}
