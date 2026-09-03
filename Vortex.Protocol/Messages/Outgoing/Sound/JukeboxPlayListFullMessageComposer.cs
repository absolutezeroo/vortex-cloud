using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Sound;

/// <summary>
/// "This jukebox is full." Carries nothing, and correctly so: the client's handler
/// (<c>onJukeboxPlayListFullMessage</c>) reads no fields and raises its own dialog.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record JukeboxPlayListFullMessageComposer : IComposer;
