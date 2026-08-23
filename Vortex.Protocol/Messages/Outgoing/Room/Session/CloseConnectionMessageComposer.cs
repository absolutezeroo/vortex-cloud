using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Room.Session;

[GenerateSerializer, Immutable]
public sealed record CloseConnectionMessageComposer : IComposer;
