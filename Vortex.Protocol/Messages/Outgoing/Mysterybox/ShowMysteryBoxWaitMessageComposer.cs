using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Mysterybox;

/// <summary>Opens the "waiting for the other half" dialog. Payload-less: the client decides what to
/// draw from whether the viewer owns the box furniture plus its own cached mystery box/key colours.</summary>
[GenerateSerializer, Immutable]
public sealed record ShowMysteryBoxWaitMessageComposer : IComposer { }
