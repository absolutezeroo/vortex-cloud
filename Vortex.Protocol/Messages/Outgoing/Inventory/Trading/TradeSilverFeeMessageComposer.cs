using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Inventory.Trading;

/// <summary>
/// The silver fee this trade must cover before it can be confirmed (header 3497).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_2546/_SafeCls_4291.as): a single int. The parser
/// resets to -1 rather than 0, so "no answer yet" is distinguishable from "no fee".
/// </summary>
[GenerateSerializer, Immutable]
public sealed record TradeSilverFeeMessageComposer : IComposer
{
    [Id(0)]
    public required int SilverFee { get; init; }
}
