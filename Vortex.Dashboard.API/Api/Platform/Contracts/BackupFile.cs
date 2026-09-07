using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>One dump on disk.</summary>
public sealed record BackupFile(string FileName, long SizeBytes, DateTime CreatedUtc);
