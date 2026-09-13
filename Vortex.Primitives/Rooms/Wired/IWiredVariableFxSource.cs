using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Primitives.Rooms.Wired;

/// <summary>
/// An add-on that hangs a Variable FX display off the variable box it is stacked with.
/// </summary>
/// <remarks>
/// Distinct from <see cref="IWiredSubVariableSource"/>, and deliberately: the level-up and time
/// add-ons <em>add variables</em>, while these six add a way of drawing one that already exists.
/// Nothing about a health bar is readable as a variable, so it has no business in the variable
/// registry.
/// </remarks>
public interface IWiredVariableFxSource
{
    /// <summary>
    /// The display this add-on declares for <paramref name="parent"/>, or null when its
    /// configuration does not describe one yet.
    /// </summary>
    WiredVariableFxConfigSnapshot? CreateFxConfig(IWiredVariable parent);
}
