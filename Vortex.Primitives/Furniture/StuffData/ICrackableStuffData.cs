namespace Vortex.Primitives.Furniture.StuffData;

/// <summary>
/// Furniture that takes several hits before it gives anything up (eggs, piñatas, tents). The client
/// renders the damage from the state string and shows "<c>hits</c> of <c>target</c> remaining" in the
/// infostand, but it decides nothing: both counters arrive from the server on every update.
/// </summary>
public interface ICrackableStuffData : IStuffData
{
    /// <summary>Hits landed so far.</summary>
    public int Hits { get; }

    /// <summary>Hits needed before the prize is handed out.</summary>
    public int Target { get; }

    /// <summary>Records one hit and returns the new total.</summary>
    public int AddHit();

    /// <summary>Sets how many hits this instance needs, from its prize binding.</summary>
    public void SetTarget(int target);
}
