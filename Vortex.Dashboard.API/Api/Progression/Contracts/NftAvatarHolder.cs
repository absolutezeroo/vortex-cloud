using System;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One copy of a wearable avatar, and whether it is being worn.</summary>
public sealed record NftAvatarHolder(
    int Id,
    int PlayerId,
    string? PlayerName,
    int SerialNumber,
    string? GrantNote,
    DateTime GrantedAt,
    bool Worn
);
