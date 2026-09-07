using System.Collections.Generic;
using Vortex.Dashboard.API.Api.Platform;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// Every tunable key with its catalogue metadata and its current stored value.
/// </summary>
/// <remarks>
/// The config grain only knows keys somebody has written, so a row is the descriptor joined with
/// the live value -- which is how the editor can show a key that has never been set.
/// </remarks>
public sealed record ConfigList(int Count, IReadOnlyList<ConfigEntryDto> Items);
