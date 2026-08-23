using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Callforhelp;

/// <summary>
/// The player's own sanction history (header 1746) - one entry per alert, mute or ban on record.
///
/// Shape from WIN63's parser (unknowns/_SafePkg_2056/_SafeCls_2564.as): a count, then that many
/// records. Each record nests two sanction types, the one being served and the one that would come
/// next, and the pair is not symmetrical - the current type is read first and the next type last,
/// with the three scalars in between.
///
/// Beware older references to this message: it used to be thirteen flat fields, and a parser for
/// that dead shape survives in the win63_version dump. The client's own parser was written against
/// it and could not read a real packet; both were corrected on 2026-08-12.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record SanctionStatusEventMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<SanctionRecord> Sanctions { get; init; }
}

/// <summary>
/// One entry in the history. Description is the only field the client always prints; the rest is
/// expanded into extra lines when ShowsProbationDetails is set.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record SanctionRecord
{
    [Id(0)]
    public required SanctionType SanctionType { get; init; }

    [Id(1)]
    public required string Description { get; init; }

    /// <summary>
    /// Turns the row into the "you are on probation, N days left, next time X" block.
    /// </summary>
    [Id(2)]
    public required bool ShowsProbationDetails { get; init; }

    /// <summary>Hours, not days - the client rounds up to whole days for display.</summary>
    [Id(3)]
    public required int ProbationHoursLeft { get; init; }

    [Id(4)]
    public required SanctionType NextSanctionType { get; init; }
}

/// <summary>
/// A sanction kind. Name is matched against ALERT / MUTE / BAN_PERMANENT by the client, which falls
/// through to a generic ban for anything else.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record SanctionType
{
    [Id(0)]
    public required string Name { get; init; }

    /// <summary>
    /// How long the sanction lasts, in hours. Quoted as hours for a mute and divided by 24 for a
    /// ban.
    /// </summary>
    [Id(1)]
    public required int DurationHours { get; init; }

    /// <summary>
    /// Read by the client but never used by it, so the meaning cannot be recovered from a call site
    /// the way DurationHours can - the name is a guess. It must still be written, or the next
    /// record decodes four bytes out of step.
    /// </summary>
    [Id(2)]
    public int ProbationHours { get; init; }
}
