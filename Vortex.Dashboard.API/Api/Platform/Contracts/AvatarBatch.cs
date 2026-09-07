using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// The avatar heads for a batch of player ids.
/// </summary>
/// <remarks>
/// Exists so that every surface already showing a player id can draw the real head without each of
/// those endpoints loading Figure and emitting its own url. The front end batches the ids it needs
/// per tick and caches what comes back.
/// </remarks>
public sealed record AvatarBatch(IReadOnlyList<AvatarBatchRow> Items);
