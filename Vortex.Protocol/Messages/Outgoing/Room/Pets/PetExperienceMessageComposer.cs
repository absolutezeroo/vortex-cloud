using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Pets;

[GenerateSerializer, Immutable]
public sealed record PetExperienceMessageComposer : IComposer
{
    [Id(0)]
    public required int PetId { get; init; }

    /// <summary>The pet's room object id. The client floats the gain over the pet it finds in the
    /// room by this, so it is not the pet row id.</summary>
    [Id(1)]
    public required int PetRoomIndex { get; init; }

    /// <summary>How much was just gained -- the delta, not the total. This message is only the
    /// floating "+N"; the level, totals and thresholds belong to PetInfo, which already carries
    /// them and which the client reads when the pet's info dialog is opened.</summary>
    [Id(2)]
    public required int GainedExperience { get; init; }
}
