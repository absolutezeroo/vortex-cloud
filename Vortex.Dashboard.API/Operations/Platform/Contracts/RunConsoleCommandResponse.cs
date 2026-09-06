using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

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
