using System;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>The period actually searched, echoed back so the page can show what it got.</summary>
public sealed record ChatlogWindow(DateTime Since, DateTime Until);
