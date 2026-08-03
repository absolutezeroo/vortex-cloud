namespace Vortex.Rooms.Configuration;

public class PetConfig
{
    public int TickMs { get; init; } = 500;
    public int WanderIdleMinMs { get; init; } = 2500;
    public int WanderIdleMaxMs { get; init; } = 7000;
    public int WanderRadius { get; init; } = 5;
    public int WanderCandidateAttempts { get; init; } = 12;
    public int NutritionCap { get; init; } = 100;
    public int EnergyCap { get; init; } = 100;
    public double NutritionDecayPerMinute { get; init; } = 1.0;
    public double EnergyDecayPerMinute { get; init; } = 0.5;
    public int StatFlushIntervalMs { get; init; } = 60_000;

    /// <summary>Energy at which a sleeping pet has rested enough to get back up.</summary>
    public int SleepWakeEnergyThreshold { get; init; } = 40;

    /// <summary>
    /// Energy at which a pet reads as tired: it stops wandering, heads for the nearest free nest and
    /// naps there. Must stay below <see cref="SleepWakeEnergyThreshold" />, otherwise a pet would
    /// wake on the same tick it fell asleep.
    /// </summary>
    public int TiredEnergyThreshold { get; init; } = 20;

    public double NestEnergyMultiplier { get; init; } = 2.0;
    public string NestLogicName { get; init; } = "pet_nest";
    public string FoodLogicName { get; init; } = "pet_food";
    public string DrinkLogicName { get; init; } = "pet_drink";
    public int HungerThreshold { get; init; } = 50;
    public int ThirstThreshold { get; init; } = 50;
    public int VocalIntervalMs { get; init; } = 14_000;
    public int MaxWellBeingSeconds { get; init; } = 86_400;
    public int RespectDailyCapPerPet { get; init; } = 3;

    /// <summary>
    /// How old an account must be, in days, before it may respect a pet. Habbo gates this; the
    /// default of 0 leaves the gate open, so a hotel opts in rather than inheriting a rule it never
    /// asked for.
    /// </summary>
    public int RespectMinimumAccountAgeDays { get; init; }
    public int RespectXpReward { get; init; } = 5;
    public int CommandXpReward { get; init; } = 3;
    public int SupplementEnergyBoost { get; init; } = 30;
    public int SupplementXpReward { get; init; } = 5;
}
