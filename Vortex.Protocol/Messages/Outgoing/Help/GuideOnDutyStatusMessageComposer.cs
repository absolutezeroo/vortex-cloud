using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Help;

/// <summary>
/// The guide tool's header: whether this player is on duty, and how many of each role are covering
/// the queues right now.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GuideOnDutyStatusMessageComposer : IComposer
{
    [Id(0)]
    public required bool OnDuty { get; init; }

    [Id(1)]
    public required int GuidesOnDuty { get; init; }

    [Id(2)]
    public required int HelpersOnDuty { get; init; }

    [Id(3)]
    public required int GuardiansOnDuty { get; init; }
}
