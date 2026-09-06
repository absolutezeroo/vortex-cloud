using System.Globalization;

namespace Vortex.Primitives.Signals;

/// <summary>
/// Formats a value the way every signal must carry it.
/// </summary>
/// <remarks>
/// Invariant culture, always. A target and a fact are compared against content written once and
/// served by every silo, so a locale that formats numbers differently would make the same task match
/// on one machine and not on another — the kind of failure that only shows up after a deployment to
/// a differently-configured host.
/// </remarks>
public static class SignalValue
{
    /// <summary>An identifier as a signal carries it.</summary>
    public static string Id(long value) => value.ToString(CultureInfo.InvariantCulture);
}
