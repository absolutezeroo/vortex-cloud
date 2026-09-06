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

/// <summary>
///     A command line typed into the dashboard console. Unlike writing to a process's stdin, this
///     carries a reason and an actor, and both end up in the audit trail.
/// </summary>
public sealed record RunConsoleCommandRequest(string Command, string Reason) : IReasonedRequest;

/// <summary>
///     The audited outcome plus whatever the command printed. Deliberately flat rather than wrapping
///     an <see cref="OperationResult"/>: every dashboard write is posted through one client helper
///     that reads <c>ok</c>, <c>correlationId</c> and <c>message</c> off the top level, and a nested
///     result would read to it as a failure with no message.
///     <para>
///     The lines matter on their own — a command that answers "No player named 'bob'." succeeded as
///     an operation and still failed as an intent, and the operator needs to read that.
///     </para>
/// </summary>
public sealed record RunConsoleCommandResponse(
    bool Ok,
    string CorrelationId,
    string Message,
    IReadOnlyList<string> Output
)
{
    public static RunConsoleCommandResponse From(
        OperationResult result,
        IReadOnlyList<string> output
    ) => new(result.Ok, result.CorrelationId, result.Message, output);
}
