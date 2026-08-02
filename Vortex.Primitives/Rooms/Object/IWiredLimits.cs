namespace Vortex.Primitives.Rooms.Object;

/// <summary>
/// The wired tuning knobs furniture logic reads. The room's configuration object itself is a
/// room-side type, so this is the slice of it that crosses into the object contracts -- five
/// scalars, no more.
/// </summary>
public interface IWiredLimits
{
    /// <summary>Largest area, in tiles, an area selector may cover.</summary>
    int WiredSelectorMaxAreaSize { get; }

    /// <summary>How many items a wired box may hold in its selection.</summary>
    int WiredSelectedItemsLimit { get; }

    /// <summary>Radius, in tiles, of the neighbourhood selector.</summary>
    int WiredNeighborhoodRadius { get; }

    /// <summary>Upper bound on the integer parameters a wired box accepts.</summary>
    int WiredMaxIntParams { get; }

    /// <summary>Whether wired boxes may act on wall furniture.</summary>
    bool WiredAllowWallFurni { get; }
}
