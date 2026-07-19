using Microsoft.Extensions.Logging;

namespace Vortex.Logging.Extensions;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddVortexConsoleLogger(this ILoggingBuilder builder)
    {
        builder.AddConsoleFormatter<VortexConsoleFormatter, VortexConsoleFormatterOptions>();
        builder.AddConsole(opts =>
        {
            opts.FormatterName = VortexConsoleFormatter.FORMATTER_NAME;
        });

        return builder;
    }
}
