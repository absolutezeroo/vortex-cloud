using Microsoft.Extensions.Logging;

namespace Vortex.Logging.Factories;

public interface IPrefixedLoggerFactory
{
    ILogger CreateLogger(string categoryName);
}
