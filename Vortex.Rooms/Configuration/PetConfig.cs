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

    /// <summary>
    /// Mood drains on its own clock while the pet is up, and comes back while it rests. Two a minute
    /// awake is what Habbo does (one every thirty seconds); resting pays it back twice as fast, so a
    /// nap is worth taking.
    /// </summary>
    public double HappinessDecayPerMinute { get; init; } = 2.0;
    public double HappinessRestGainPerMinute { get; init; } = 4.0;
    public int HappinessCap { get; init; } = 100;
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

    /// <summary>Obeying pleases a pet. Habbo pays 5 to 25 depending on how fun the trick is.</summary>
    public int CommandHappinessReward { get; init; } = 5;

    /// <summary>What a toy is worth. Habbo's guides say toys are what cheer a pet up; 25 is the
    /// figure Arcturus pays for a ball or a trampoline.</summary>
    public int ToyHappinessReward { get; init; } = 25;

    /// <summary>Below this, a pet goes looking for a toy rather than wandering aimlessly.</summary>
    public int BoredHappinessThreshold { get; init; } = 50;
    public int SupplementEnergyBoost { get; init; } = 30;
    public int SupplementXpReward { get; init; } = 5;
}
