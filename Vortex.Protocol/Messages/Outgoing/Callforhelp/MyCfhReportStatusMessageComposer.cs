using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Callforhelp;

/// <summary>
/// The reports the player filed (header 3809), listed back to them by the "my reports" window.
///
/// Shape from WIN63's parser (unknowns/_SafePkg_2056/_SafeCls_3848.as + _SafeCls_2648.as): a count,
/// then that many records of eleven fields. The client opens the window on this reply and has no
/// timeout, so an unanswered request is a window that never appears rather than an empty one.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record MyCfhReportStatusMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<CfhReportStatusSnapshot> Reports { get; init; }
}
