using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One bot, with the chatter phrases spelled out rather than counted.
/// </summary>
/// <param name="MixSentences">The client's markov flag: the bot recombines its phrases instead of
/// reciting them.</param>
/// <param name="RawSkillsJson">The blob exactly as stored. A configuration the decoder does not
/// understand is what an operator investigating a silent bot has come here to see.</param>
public sealed record BotDetail(
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
    IReadOnlyList<string> Phrases,
    bool AutoChat,
    int ChatDelaySeconds,
    bool MixSentences,
    bool Wanders,
    bool Dances,
    string? RawSkillsJson,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
