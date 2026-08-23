using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Engine;

/// <summary>
/// Asks the player to confirm removing a placed object (header 3643).
///
/// Shape from WIN63's parser (unknowns/.../_SafeCls_3699.as): two ints and two strings. The first
/// int is a *flag*, not a category - the client turns 1 into wall-item and anything else into
/// floor-item, so send 1 for a wall item and 0 otherwise.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record ObjectRemoveConfirmMessageComposer : IComposer
{
    /// <summary>1 for a wall item, 0 for a floor item.</summary>
    [Id(0)]
    public required int IsWallItem { get; init; }

    /// <summary>The object's id.</summary>
    [Id(1)]
    public required int ObjectId { get; init; }

    /// <summary>Title of the confirmation dialog.</summary>
    [Id(2)]
    public required string ConfirmTitle { get; init; }

    /// <summary>Body of the confirmation dialog.</summary>
    [Id(3)]
    public required string ConfirmBody { get; init; }
}
