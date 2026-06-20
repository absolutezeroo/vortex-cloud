using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Turbo.Logging;

internal sealed class TurboConsoleFormatter(
    IOptionsMonitor<TurboConsoleFormatterOptions> optionsMonitor
) : ConsoleFormatter(FORMATTER_NAME)
{
    public const string FORMATTER_NAME = "turbo";

    private readonly IOptionsMonitor<TurboConsoleFormatterOptions> _optionsMonitor = optionsMonitor;

    // Level label & color mappings
    private static readonly Dictionary<LogLevel, string> LOG_LEVEL_LABELS = new()
    {
        [LogLevel.Trace] = "TRC",
        [LogLevel.Debug] = "DBG",
        [LogLevel.Information] = "INF",
        [LogLevel.Warning] = "WRN",
        [LogLevel.Error] = "ERR",
        [LogLevel.Critical] = "CRT",
        [LogLevel.None] = "NON",
    };

    private static readonly Dictionary<LogLevel, (string fg, string bg)> LOG_LEVEL_COLORS = new()
    {
        [LogLevel.Trace] = ("\u001b[90m", ""), // Bright Black (Gray)
        [LogLevel.Debug] = ("\u001b[36m", ""), // Cyan
        [LogLevel.Information] = ("\u001b[37m", ""), // White
        [LogLevel.Warning] = ("\u001b[33m", ""), // Yellow
        [LogLevel.Error] = ("\u001b[31m", ""), // Red
        [LogLevel.Critical] = ("\u001b[97m", "\u001b[41m"), // White on Red background
        [LogLevel.None] = ("\u001b[37m", ""),
    };

    private const string RESET = "\u001b[0m";

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter
    )
    {
        TurboConsoleFormatterOptions options = _optionsMonitor.CurrentValue;

        DateTimeOffset now = options.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
        string ts = now.ToString(options.TimestampFormat ?? "yyyy-MM-dd HH:mm:ss.fff");

        LogLevel level = logEntry.LogLevel;
        string levelLabel = LOG_LEVEL_LABELS.TryGetValue(level, out string? lbl)
            ? lbl
            : level.ToString().ToUpperInvariant();

        string? category = null;
        if (options.IncludeCategory && !string.IsNullOrEmpty(logEntry.Category))
        {
            category = TrimCategory(logEntry.Category!, options.TrimCategoryDepth);
        }

        (string fg, string bg) = LOG_LEVEL_COLORS[level];

        // Build header (timestamp + level + category)
        StringBuilder headerSb = new StringBuilder(128);
        if (options.UseAnsiColor)
        {
            headerSb.Append('[').Append(ts).Append("] ");
            if (bg.Length > 0)
            {
                headerSb.Append(bg);
            }

            headerSb.Append(fg).Append(RESET);
            if (!string.IsNullOrEmpty(bg))
            {
                headerSb.Append(RESET);
            }
        }
        else
        {
            headerSb.Append('[').Append(ts).Append("] ");
        }

        if (!string.IsNullOrEmpty(category))
        {
            headerSb.Append(category);
        }

        if (logEntry.EventId.Id != 0)
        {
            headerSb.Append(" [").Append(logEntry.EventId.Id).Append(']');
        }

        string header = headerSb.ToString();

        // ===== Align after the category =====
        // Assume max width for the header column
        const int HEADER_WIDTH = 55;
        string paddedHeader = header.PadRight(HEADER_WIDTH);

        string? message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (!string.IsNullOrEmpty(message))
        {
            if (options.SingleLine)
            {
                message = message
                    .Replace(Environment.NewLine, " ")
                    .Replace('\n', ' ')
                    .Replace('\r', ' ');
            }
        }

        StringBuilder sb = new StringBuilder(256);
        sb.Append(paddedHeader);

        if (!string.IsNullOrEmpty(message))
        {
            if (options.UseAnsiColor)
            {
                sb.Append(": ").Append(fg).Append(message).Append(RESET);
            }
            else
            {
                sb.Append(": ").Append(message);
            }
        }

        if (options.IncludeScopes && scopeProvider is not null)
        {
            scopeProvider.ForEachScope(
                (scope, s) =>
                {
                    s.Append(" => ").Append(scope);
                },
                sb
            );
        }

        // Write the line
        textWriter.WriteLine(sb.ToString());

        // Write the exception (on next line, aligned as well)
        if (logEntry.Exception is not null)
        {
            if (options.UseAnsiColor)
            {
                textWriter.WriteLine(
                    $"{LOG_LEVEL_COLORS[LogLevel.Error].fg}{logEntry.Exception}{RESET}"
                );
            }
            else
            {
                textWriter.WriteLine($"{logEntry.Exception}");
            }
        }
    }

    private static string TrimCategory(string category, int depth)
    {
        if (depth <= 0)
        {
            return category;
        }

        // Split by '.' and take last N segments
        string[] parts = category.Split('.');
        if (parts.Length <= depth)
        {
            return category;
        }

        int start = parts.Length - depth;
        return string.Join('.', parts, start, depth);
    }
}
