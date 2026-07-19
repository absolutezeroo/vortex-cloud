using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Vortex.Logging;

internal sealed class VortexConsoleFormatter(
    IOptionsMonitor<VortexConsoleFormatterOptions> optionsMonitor
) : ConsoleFormatter(FORMATTER_NAME)
{
    public const string FORMATTER_NAME = "vortex";

    private readonly IOptionsMonitor<VortexConsoleFormatterOptions> _optionsMonitor =
        optionsMonitor;

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

    // (badge color, message color)
    private static readonly Dictionary<LogLevel, (string Badge, string Message)> LOG_LEVEL_COLORS =
        new()
        {
            [LogLevel.Trace] = ("[90m", "[90m"), // gray
            [LogLevel.Debug] = ("[36m", "[36m"), // cyan
            [LogLevel.Information] = ("[92m", "[0m"), // bright green badge, default message
            [LogLevel.Warning] = ("[93m", "[33m"), // bright yellow badge, yellow message
            [LogLevel.Error] = ("[91m", "[31m"), // bright red badge, red message
            [LogLevel.Critical] = ("[97;41m", "[91m"), // white-on-red badge, bright red message
            [LogLevel.None] = ("[37m", "[0m"),
        };

    private const string RESET = "[0m";
    private const string DIM = "[2m";
    private const int CATEGORY_WIDTH = 22;

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter
    )
    {
        VortexConsoleFormatterOptions options = _optionsMonitor.CurrentValue;

        DateTimeOffset now = options.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
        string ts = now.ToString(options.TimestampFormat ?? "HH:mm:ss.fff");

        LogLevel level = logEntry.LogLevel;
        string levelLabel = LOG_LEVEL_LABELS.TryGetValue(level, out string? lbl)
            ? lbl
            : level.ToString().ToUpperInvariant()[..3];

        (string badge, string messageColor) = LOG_LEVEL_COLORS.TryGetValue(
            level,
            out (string Badge, string Message) c
        )
            ? c
            : (string.Empty, string.Empty);

        string? category = null;
        if (options.IncludeCategory && !string.IsNullOrEmpty(logEntry.Category))
        {
            category = TrimCategory(logEntry.Category!, options.TrimCategoryDepth);
        }

        string? message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (!string.IsNullOrEmpty(message) && options.SingleLine)
        {
            message = message
                .Replace(Environment.NewLine, " ")
                .Replace('\n', ' ')
                .Replace('\r', ' ');
        }

        // Pad or truncate category to a fixed visual width for column alignment.
        string categorySlot =
            category is null ? new string(' ', CATEGORY_WIDTH)
            : category.Length > CATEGORY_WIDTH ? category[..(CATEGORY_WIDTH - 1)] + "…"
            : category.PadRight(CATEGORY_WIDTH);

        StringBuilder sb = new StringBuilder(256);

        if (options.UseAnsiColor)
        {
            // [timestamp]
            sb.Append(DIM).Append('[').Append(ts).Append(']').Append(RESET).Append(' ');
            // LVL badge
            sb.Append(badge).Append(levelLabel).Append(RESET).Append(' ');
            // Category (dim)
            sb.Append(DIM).Append(categorySlot).Append(RESET);
            // Separator
            sb.Append(": ");
            // Message
            if (!string.IsNullOrEmpty(message))
            {
                sb.Append(messageColor).Append(message).Append(RESET);
            }
        }
        else
        {
            sb.Append('[').Append(ts).Append("] ");
            sb.Append(levelLabel).Append(' ');
            sb.Append(categorySlot);
            sb.Append(": ");
            if (!string.IsNullOrEmpty(message))
            {
                sb.Append(message);
            }
        }

        if (options.IncludeScopes && scopeProvider is not null)
        {
            scopeProvider.ForEachScope((scope, s) => s.Append(" => ").Append(scope), sb);
        }

        textWriter.WriteLine(sb.ToString());

        if (logEntry.Exception is not null)
        {
            string exLine = options.UseAnsiColor
                ? $"{LOG_LEVEL_COLORS[LogLevel.Error].Badge}{logEntry.Exception}{RESET}"
                : logEntry.Exception.ToString();
            textWriter.WriteLine(exLine);
        }
    }

    private static string TrimCategory(string category, int depth)
    {
        if (depth <= 0)
        {
            return category;
        }

        string[] parts = category.Split('.');
        if (parts.Length <= depth)
        {
            return category;
        }

        int start = parts.Length - depth;
        return string.Join('.', parts, start, depth);
    }
}
