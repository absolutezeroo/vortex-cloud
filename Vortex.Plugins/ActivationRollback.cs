using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Vortex.Plugins;

/// <summary>
/// A compensation stack for one plugin activation.
/// <para>
/// Activating a plugin is a sequence of side effects — an assembly load context, a plugin instance,
/// a service provider, exported services, migrations, hosted services, the plugin's own
/// <c>StartAsync</c>, assembly feature registrations. Any of them can fail. Each successful step
/// pushes how to undo itself here, and <see cref="UnwindAsync" /> runs those compensations in
/// reverse, so a failure at step N leaves the host exactly as it was before step 1 instead of
/// half-activated.
/// </para>
/// <para>
/// Cleanup is best effort — one compensation throwing must not prevent the rest from running — but
/// never silent: every failed step is logged with the plugin key and the step name, because a
/// rollback that quietly fails is how a leaked service provider keeps an
/// <c>AssemblyLoadContext</c> alive forever.
/// </para>
/// </summary>
internal sealed class ActivationRollback(ILogger logger, string pluginKey)
{
    private readonly List<(string Step, Func<Task> Undo)> _steps = [];

    /// <summary>Registers how to undo a step that has just succeeded.</summary>
    public void Push(string step, Func<Task> undo) => _steps.Add((step, undo));

    /// <summary>Registers a synchronous compensation.</summary>
    public void Push(string step, Action undo) =>
        _steps.Add(
            (
                step,
                () =>
                {
                    undo();

                    return Task.CompletedTask;
                }
            )
        );

    /// <summary>
    /// The activation succeeded: the envelope now owns everything, so drop the compensations
    /// without running them.
    /// </summary>
    public void Commit() => _steps.Clear();

    /// <summary>Runs every registered compensation in reverse order, logging each failure.</summary>
    public async Task UnwindAsync()
    {
        if (_steps.Count == 0)
        {
            return;
        }

        logger.LogWarning(
            "Rolling back a failed activation of plugin {PluginKey}: undoing {StepCount} completed "
                + "step(s) in reverse order.",
            pluginKey,
            _steps.Count
        );

        for (int i = _steps.Count - 1; i >= 0; i--)
        {
            (string step, Func<Task> undo) = _steps[i];

            try
            {
                await undo().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Rollback step '{Step}' failed for plugin {PluginKey}. Cleanup continues with "
                        + "the remaining steps, but this resource may have leaked.",
                    step,
                    pluginKey
                );
            }
        }

        _steps.Clear();
    }
}
