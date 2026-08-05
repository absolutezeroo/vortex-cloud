using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// A toy. A pet that plays with one cheers up, which is the only thing besides resting and obeying
/// that lifts its mood.
/// </summary>
/// <remarks>
/// Both Arcturus names are registered: it keeps balls and trampolines apart because a trampoline
/// bounces the pet, which Vortex does not model -- to a pet looking for something to play with they
/// are the same furni. The four toys in the shipped catalogue carry neither name, they were left on
/// <c>default</c>, so the seed binds them by id.
/// </remarks>
[RoomObjectLogic("pet_toy")]
[RoomObjectLogic("pet_trampoline")]
public class FurniturePetToyLogic(IStuffDataFactory stuffDataFactory, IRoomFloorItemContext ctx)
    : FurnitureFloorLogic(stuffDataFactory, ctx) { }
