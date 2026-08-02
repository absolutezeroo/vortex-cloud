using Orleans;

namespace Vortex.Primitives.Rooms.Grains;

/// <summary>
/// The wired subsystem's room-scoped surface: permanent variables, variable holders, the wired
/// room settings/stats dialogs, and the room and error logs.
/// <para>
/// Declared here and filled in by the <c>IRoomWired.*</c> parts, which keep the one-file-per-wired-menu
/// layout the implementation uses. Note that the wired <em>engine</em> itself is not exposed here — it
/// runs inside the room tick; this facet is only what the wired menus read and write.
/// </para>
/// </summary>
[Alias("Vortex.Primitives.Rooms.Grains.IRoomWired")]
public partial interface IRoomWired : IGrainWithIntegerKey { }
