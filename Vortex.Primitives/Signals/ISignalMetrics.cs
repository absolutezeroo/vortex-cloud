namespace Vortex.Primitives.Signals;

/// <summary>
/// The three questions you cannot answer from logs when progression quietly stops.
/// </summary>
/// <remarks>
/// <para>
/// A progression system that stops advancing raises no exception: content simply never completes,
/// and it is found through a player complaint weeks later. These counters are what turns that into
/// something a scrape shows.
/// </para>
/// <para>
/// The gate is counted per <em>translator</em>, not per action, because it runs before
/// <see cref="ISignalTranslator{TEvent}.Translate"/> — at that moment no signal exists and no action
/// has been chosen, and a translator may declare several. Its unit is the event; the signal's unit
/// is the action.
/// </para>
/// </remarks>
public interface ISignalMetrics
{
    /// <summary>An event the gate stopped before translating.</summary>
    void TranslationGated(string translator);

    /// <summary>An event that got through the gate and published a batch. The gate's denominator.</summary>
    void BatchPublished(string translator);

    /// <summary>
    /// One signal in a published batch. A declared action whose counter stays at zero for a week is
    /// either content nobody triggers or a translator that does not translate.
    /// </summary>
    void SignalRaised(string action);

    /// <summary>A consumer threw. The pipeline isolates it, so nothing else says so.</summary>
    void ConsumerFailed(string consumer);
}

/// <summary>Counts nothing. The fallback when observability is not wired, and the test default.</summary>
public sealed class NullSignalMetrics : ISignalMetrics
{
    /// <summary>The shared instance; it holds no state.</summary>
    public static readonly NullSignalMetrics Instance = new();

    public void TranslationGated(string translator) { }

    public void BatchPublished(string translator) { }

    public void SignalRaised(string action) { }

    public void ConsumerFailed(string consumer) { }
}
