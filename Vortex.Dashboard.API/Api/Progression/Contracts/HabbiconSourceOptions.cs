using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// The acquisition sources, so the grant form offers the real vocabulary rather than a free string.
/// </summary>
/// <remarks>
/// An operator grant is always recorded as AdminGrant whatever the form says; this list is for
/// reading the ownership table, not for choosing.
/// </remarks>
public sealed record HabbiconSourceOptions(int Count, IReadOnlyList<HabbiconSourceOption> Items);
