using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// The database dumps taken so far.
/// </summary>
/// <param name="Configured">False when no dump path is set, which is a different state from "none
/// taken yet" and the page says so differently.</param>
public sealed record BackupList(bool Configured, IReadOnlyList<BackupFile> Items);
