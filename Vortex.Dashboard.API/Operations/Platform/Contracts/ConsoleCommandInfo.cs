using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

/// <summary>One operator command as the console page needs to render it.</summary>
/// <param name="Allowed">
///     Whether the caller asking for this list may actually run it. The server refuses regardless;
///     this only lets the page grey out what would be refused instead of inviting the attempt.
/// </param>
public sealed record ConsoleCommandInfo(
    string Name,
    string Usage,
    string Description,
    string? RequiredCapability,
    bool Allowed
);
