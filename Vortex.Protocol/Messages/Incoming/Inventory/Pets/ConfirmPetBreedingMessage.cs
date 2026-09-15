using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Inventory.Pets;

/// <summary>
/// The naming dialog's save button: <c>confirmPetBreeding(stuffId, name, petOne, petTwo)</c>
/// (ConfirmPetBreedingView.as:327). Only the first field was read until 2026-09-15, and it was
/// read as a pet id — it is the nest's furniture id, so the session lookup never matched and the
/// offspring was always named "Baby".
/// </summary>
public record ConfirmPetBreedingMessage : IMessageEvent
{
    /// <summary>The breeding nest furniture the dialog was opened from. The client keys the
    /// result event to it.</summary>
    public required int NestStuffId { get; init; }

    /// <summary>The name the player typed for the offspring.</summary>
    public required string PetName { get; init; }

    public required int PetOneId { get; init; }

    public required int PetTwoId { get; init; }
}
