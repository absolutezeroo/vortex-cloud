using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Catalog;

/// <summary>Sent when there is no targeted offer available for the player. Carries no payload.</summary>
[GenerateSerializer, Immutable]
public sealed record TargetedOfferNotFoundEventMessageComposer : IComposer { }
