using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Console;

namespace Vortex.Logging;

/// <summary>
///     Copies every line the emulator logs into the <see cref="ServerConsoleFeed"/>, so the
///     dashboard can show the same console the operator would see over SSH.
///     <para>
///     It renders through the very same <see cref="VortexConsoleFormatter"/> the terminal uses rather
///     than formatting again: a second implementation would drift, and the whole point of this
///     surface is that it looks like the console it stands in for.
///     </para>
/// </summary>
internal sealed class ServerConsoleLoggerProvider(
    ServerConsoleFeed feed,
    IOptionsMonitor<VortexConsoleFormatterOptions> formatterOptions
) : ILoggerProvider, ISupportExternalScope
{
    private readonly VortexConsoleFormatter _formatter = new(formatterOptions);

    private IExternalScopeProvider? _scopeProvider;

    public ILogger CreateLogger(string categoryName) =>
        new FeedLogger(categoryName, _formatter, feed, () => _scopeProvider);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) =>
        _scopeProvider = scopeProvider;

    public void Dispose()
    {
        // The feed outlives this provider and owns nothing disposable.
    }

    private sealed class FeedLogger(
        string category,
        ConsoleFormatter formatter,
        ServerConsoleFeed feed,
        Func<IExternalScopeProvider?> scopeProvider
    ) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => scopeProvider()?.Push(state) ?? NullScope.Instance;

        // Level filtering is the logging factory's job: it applies the configured rules before
        // calling in, and answering false here would silently override them for this sink only.
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatterFunc
        )
        {
            LogEntry<TState> entry = new(
                logLevel,
                category,
                eventId,
                state,
                exception,
                formatterFunc
            );

            using StringWriter writer = new();

            formatter.Write(in entry, scopeProvider(), writer);

            // The formatter writes whole lines, and an exception is appended as its own; split so a
            // stack trace arrives as separate rows rather than one unreadable blob.
            foreach (
                string line in writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)
            )
            {
                feed.Publish(line.TrimEnd('\r'));
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose() { }
    }
}
