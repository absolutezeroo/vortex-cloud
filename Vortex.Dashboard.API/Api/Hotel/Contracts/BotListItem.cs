using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// A bot in the roster, with its skill blob already decoded: the stored row keys everything by the
/// client's own skill ids, so the raw value tells an operator nothing.
/// </summary>
/// <param name="Placed">True when the bot stands in a room, false when it sits in its owner's hand.
/// The coordinates below mean nothing in the second case.</param>
/// <param name="RoomName">Null when the bot is not placed, and also when its room is gone.</param>
/// <param name="Skills">The client skill ids present, sorted; <paramref name="SkillNames"/> is the
/// same list named, one entry per id.</param>
public sealed record BotListItem(
    int Id,
    string Name,
    string Motto,
    string Figure,
    string? AvatarUrl,
    string Gender,
    int OwnerId,
    string? OwnerName,
    int? RoomId,
    string? RoomName,
    bool Placed,
    int X,
    int Y,
    int Z,
    int Rotation,
    IReadOnlyList<int> Skills,
    IReadOnlyList<string> SkillNames,
    int PhraseCount,
    bool AutoChat,
    int ChatDelaySeconds,
    bool Wanders,
    bool Dances,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
