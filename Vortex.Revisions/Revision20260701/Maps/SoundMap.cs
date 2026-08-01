using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Sound;

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
    }
}
