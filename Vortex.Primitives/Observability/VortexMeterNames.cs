namespace Vortex.Primitives.Observability;

/// <summary>
/// The meter names an exporter has to subscribe to in order to see anything. They live here rather
/// than next to the instruments so the producing side (<c>Vortex.Observability</c>) and the exporting
/// side (the scraping endpoint on the web API host) can agree on a name without either referencing
/// the other.
/// </summary>
public static class VortexMeterNames
{
    /// <summary>The emulator's own meter — every instrument behind <see cref="IVortexMetrics"/>.</summary>
    public const string VORTEX = "Vortex";

    /// <summary>
    /// Orleans' built-in runtime counters (grain activations, message queues, scheduler latency).
    /// Published by the framework itself; we only have to subscribe to them.
    /// </summary>
    public const string ORLEANS = "Microsoft.Orleans";
}
