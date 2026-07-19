using Microsoft.Extensions.Logging;
using Vortex.Logging.Extensions;

namespace Vortex.Logging;

public static class BootstrapLoggingFactory
{
    public static ILoggerFactory CreateBootstrapLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
        {
            builder.AddTurboConsoleLogger();
        });
    }
}
