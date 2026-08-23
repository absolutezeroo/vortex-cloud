using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

/// <summary>The trade went through; the client closes its screen. No payload.</summary>
[GenerateSerializer, Immutable]
public sealed record WiredTradeCompletedMessageComposer : IComposer;
