using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Vortex.Logging.Extensions;

/// <summary>
///     Shared fire-and-forget helper. Orleans grain code must not block on a task nor let its
///     failure vanish silently; this observes the task and logs on fault instead of relying on
///     per-call-site <c>ContinueWith(..., TaskScheduler.Current)</c>, whose semantics inside a
///     grain turn are subtle and easy to get wrong.
/// </summary>
public static class TaskLoggingExtensions
{
    public static void LogAndForget(
        this Task task,
        ILogger logger,
        string message,
        params object?[] args
    )
    {
        _ = AwaitAndLogAsync(task, logger, message, args);
    }

    // VSTHRD003: the whole point of this helper is to observe a task that's already running,
    // started elsewhere by the caller — that's the fire-and-forget contract, not a deadlock risk.
#pragma warning disable VSTHRD003
    private static async Task AwaitAndLogAsync(
        Task task,
        ILogger logger,
        string message,
        object?[] args
    )
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, message, args);
        }
    }
#pragma warning restore VSTHRD003
}
