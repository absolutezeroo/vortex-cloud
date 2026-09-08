using System;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>
/// One item whose recorded life does not add up.
/// </summary>
/// <param name="Kind">Which signature fired: <c>born_twice</c>, <c>resurrected</c> or
/// <c>two_places</c>. Stable codes, because the page groups on them and a sentence written for a
/// human is not something to group on.</param>
/// <param name="Detail">The evidence in one line — what was seen, and how often. Written here
/// rather than in the browser so the CSV an investigator exports carries it too.</param>
/// <param name="Occurrences">How many times the signature fired for this item. Two creations is
/// odd; nine is somebody's afternoon.</param>
/// <param name="DefinitionName">What the item is, when it still exists. Null for one that has since
/// been deleted -- which is not a reason to hide the anomaly, since a deleted duplicate is still a
/// duplicate that was spent.</param>
public sealed record ItemAnomaly(
    long ItemId,
    string Kind,
    string Detail,
    int Occurrences,
    DateTime FirstSeen,
    DateTime LastSeen,
    int? DefinitionId,
    string? DefinitionName,
    int? OwnerPlayerId,
    string? OwnerName
);
