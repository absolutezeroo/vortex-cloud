using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

/// <summary>The trade went through; the client closes its screen. No payload.</summary>
[GenerateSerializer, Immutable]
public sealed record WiredTradeCompletedMessageComposer : IComposer;
