using System;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One player waiting to be let into a guild.</summary>
public sealed record GuildRequestRow(int PlayerId, string? PlayerName, DateTime RequestedAt);
