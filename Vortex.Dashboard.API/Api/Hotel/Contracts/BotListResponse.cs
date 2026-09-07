using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One page of the bot roster.</summary>
public sealed record BotListResponse(
    int Page,
    int Limit,
    int Offset,
    int Total,
    int Count,
    IReadOnlyList<BotListItem> Items
);
