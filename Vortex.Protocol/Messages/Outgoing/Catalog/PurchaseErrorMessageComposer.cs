using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record PurchaseErrorMessageComposer : IComposer
{
    /// <summary>Client-side error code; 0 is the generic "purchase failed" the client falls back
    /// to. Sent unconditionally -- the client reads it whatever we put in, and used to read past the
    /// end of an empty packet.</summary>
    [Id(0)]
    public int ErrorCode { get; init; }
}
