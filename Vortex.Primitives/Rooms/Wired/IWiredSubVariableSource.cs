using System.Collections.Generic;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Primitives.Rooms.Wired;

/// <summary>
/// An add-on that gives the variable box it is stacked with more variables than the one the box
/// itself declares.
/// </summary>
/// <remarks>
/// This is how the whole "Variable ..." add-on family works on the client: a level-up add-on turns
/// one experience variable into level, progress, required and max; a time add-on turns one timestamp
/// into year, hour-of-day and the rest; the Variable FX boxes hang a display off one. They are
/// add-ons rather than variable boxes because they describe <em>a</em> variable — they have no value
/// of their own.
/// <para>
/// The room registers what this returns alongside the parent, which is why the registry keeps a list
/// of variable ids per box rather than one. Names are the parent's, extended with <c>.</c> — the
/// client's own hierarchy separator, the one <c>Util.splitName</c> cuts on.
/// </para>
/// </remarks>
public interface IWiredSubVariableSource
{
    /// <summary>
    /// The variables this add-on derives from <paramref name="parent"/>.
    /// </summary>
    /// <remarks>
    /// Called when the box is (re)processed, so the result may be rebuilt at any time and must not
    /// carry state the room is expected to keep. An add-on whose configuration names nothing returns
    /// an empty list rather than variables with blank names: the registry drops nameless variables,
    /// and a list of them is a slower way of returning none.
    /// </remarks>
    IReadOnlyList<IWiredVariable> CreateSubVariables(IWiredVariable parent);
}
