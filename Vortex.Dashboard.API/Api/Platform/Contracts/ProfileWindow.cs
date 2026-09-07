using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>The span a profile timeline and its counts were read over.</summary>
public sealed record ProfileWindow(DateTime Since, DateTime Until);
