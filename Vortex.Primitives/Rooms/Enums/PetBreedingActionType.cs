namespace Vortex.Primitives.Rooms.Enums;

/// <summary>
/// Which of the three breeding buttons the client pressed. All three send the same packet and
/// differ only by this leading integer — <c>AvatarInfoWidget.breedPets</c> sends 0,
/// <c>cancelBreedPets</c> sends 1 and <c>acceptBreedPets</c> sends 2, each followed by the two
/// pet ids (WIN63-202607011411, AvatarInfoWidget.as:1737-1764).
/// </summary>
public enum PetBreedingActionType
{
    /// <summary>Ask the other pet's owner for a breeding session.</summary>
    Request = 0,

    /// <summary>Withdraw or refuse a pending request.</summary>
    Cancel = 1,

    /// <summary>Agree to the request; the naming dialog then sends ConfirmPetBreeding.</summary>
    Accept = 2,
}
