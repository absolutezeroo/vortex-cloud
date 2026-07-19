using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.Primitives.Pets.Providers;

public interface IPetLevelProvider
{
    int GetLevelForExperience(int petType, int experience);

    int GetExperienceForNextLevel(int petType, int currentLevel);

    int GetEnergyCapForLevel(int petType, int level);

    int GetNutritionCapForLevel(int petType, int level);

    PetLevelEntry? GetEntry(int petType, int level);

    int GetMaxLevel(int petType);

    Task ReloadAsync(CancellationToken ct);
}
