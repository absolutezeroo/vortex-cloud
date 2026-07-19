using Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;

namespace Vortex.Primitives.Orleans.Snapshots.Navigator;

[GenerateSerializer, Immutable]
public record NavigatorSearchResultSnapshot : RoomInfoSnapshot { }
