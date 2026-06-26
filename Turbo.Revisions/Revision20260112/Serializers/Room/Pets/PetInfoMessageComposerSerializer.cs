using System;
using Turbo.Primitives.Messages.Outgoing.Room.Pets;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Pets.Snapshots;

namespace Turbo.Revisions.Revision20260112.Serializers.Room.Pets;

internal class PetInfoMessageComposerSerializer(int header)
    : AbstractSerializer<PetInfoMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PetInfoMessageComposer message)
    {
        PetSnapshot pet = message.Pet;

        packet
            .WriteInteger(pet.PetId)
            .WriteString(pet.Name)
            .WriteInteger(pet.Level)
            .WriteInteger(Math.Max(message.MaxLevel, pet.Level))
            .WriteInteger(pet.Experience)
            .WriteInteger(Math.Max(message.ExperienceRequiredToLevel, pet.Experience))
            .WriteInteger(pet.Energy)
            .WriteInteger(Math.Max(message.MaxEnergy, pet.Energy))
            .WriteInteger(pet.Nutrition)
            .WriteInteger(Math.Max(message.MaxNutrition, pet.Nutrition))
            .WriteInteger(pet.Respect)
            .WriteInteger(pet.OwnerId.Value)
            .WriteInteger(message.Age)
            .WriteString(message.OwnerName)
            .WriteInteger(pet.Race)
            .WriteBoolean(message.HasFreeSaddle)
            .WriteBoolean(message.IsRiding)
            .WriteInteger(message.SkillThresholds.Length);

        foreach (int threshold in message.SkillThresholds)
        {
            packet.WriteInteger(threshold);
        }

        packet
            .WriteInteger(message.AccessRights)
            .WriteBoolean(message.CanBreed)
            .WriteBoolean(message.CanHarvest)
            .WriteBoolean(message.CanRevive)
            .WriteInteger(message.RarityLevel)
            .WriteInteger(message.MaxWellBeingSeconds)
            .WriteInteger(message.RemainingWellBeingSeconds)
            .WriteInteger(message.RemainingGrowingSeconds)
            .WriteBoolean(message.HasBreedingPermission);
    }
}
