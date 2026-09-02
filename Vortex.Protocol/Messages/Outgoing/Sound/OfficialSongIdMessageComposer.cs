using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Sound;

[GenerateSerializer, Immutable]
public sealed record OfficialSongIdMessageComposer : IComposer
{
    // TODO: the wire shape IS identified — AS3's parser (_SafePkg_2899/_SafeCls_4287.parse())
    // reads, in order: string officialSongId, int songId. Left unfilled only because nothing
    // constructs this yet: GetOfficialSongIdMessageHandler is an accept-and-drop stub and the
    // server has no song storage at all (every handler under Vortex.PacketHandlers/Sound is).
    // Fill both properties and the serializer together with that handler.
}
