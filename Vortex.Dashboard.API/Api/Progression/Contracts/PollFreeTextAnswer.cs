using System;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One player's typed answer, with enough to show who wrote it.</summary>
public sealed record PollFreeTextAnswer(
    int PlayerId,
    string? PlayerName,
    string? AvatarUrl,
    string Answer,
    DateTime AnsweredAt
);
