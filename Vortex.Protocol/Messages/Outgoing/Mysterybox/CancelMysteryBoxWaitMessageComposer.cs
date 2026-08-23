using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Mysterybox;

/// <summary>Closes an open mystery box wait dialog. Payload-less.</summary>
[GenerateSerializer, Immutable]
public sealed record CancelMysteryBoxWaitMessageComposer : IComposer { }
