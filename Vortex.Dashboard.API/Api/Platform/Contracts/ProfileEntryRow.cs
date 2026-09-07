using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>One room the account walked into.</summary>
public sealed record ProfileEntryRow(DateTime CreatedAt, int RoomId, string? RoomName);
