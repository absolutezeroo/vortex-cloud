using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Platform.Contracts;

/// <summary>
///     A command line typed into the dashboard console. Unlike writing to a process's stdin, this
///     carries a reason and an actor, and both end up in the audit trail.
/// </summary>
public sealed record RunConsoleCommandRequest(string Command, string Reason) : IReasonedRequest;
