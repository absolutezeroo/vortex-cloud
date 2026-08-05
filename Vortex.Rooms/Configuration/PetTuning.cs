using System.Threading.Tasks;
using Vortex.Primitives.Server.Grains;

namespace Vortex.Rooms.Configuration;

/// <summary>
/// The pet numbers an operator will want to argue with, served live from
/// <see cref="IServerConfigGrain" /> so they can be retuned from the dashboard without a restart.
/// </summary>
/// <remarks>
/// <para>
/// None of these rates are published anywhere. Habbo's own guides describe the behaviour -- a tired
/// pet goes to its basket, toys and commands cheer it up, food restores energy -- but never a speed,
/// and the values below are read off Arcturus, which is itself a reimplementation. They are informed
/// guesses, which is exactly the kind of number that belongs in a table an admin can edit rather
/// than compiled into the build.
/// </para>
/// <para>
/// <see cref="PetConfig" /> keeps the defaults, so an untouched hotel behaves the same as before and
/// a cleared key falls back rather than reading zero.
/// </para>
/// </remarks>
public sealed record PetTuning
{
    public const string NutritionDecayPerMinuteKey = "pets.nutrition_decay_per_minute";
    public const string EnergyDecayPerMinuteKey = "pets.energy_decay_per_minute";
    public const string HappinessDecayPerMinuteKey = "pets.happiness_decay_per_minute";
    public const string HappinessRestGainPerMinuteKey = "pets.happiness_rest_gain_per_minute";
    public const string TiredEnergyThresholdKey = "pets.tired_energy_threshold";
    public const string SleepWakeEnergyThresholdKey = "pets.sleep_wake_energy_threshold";
    public const string HungerThresholdKey = "pets.hunger_threshold";
    public const string ThirstThresholdKey = "pets.thirst_threshold";
    public const string CommandHappinessRewardKey = "pets.command_happiness_reward";
    public const string ToyHappinessRewardKey = "pets.toy_happiness_reward";
    public const string BoredHappinessThresholdKey = "pets.bored_happiness_threshold";
    public const string WanderIdleMinMsKey = "pets.wander_idle_min_ms";
    public const string WanderIdleMaxMsKey = "pets.wander_idle_max_ms";
    public const string VocalIntervalMsKey = "pets.vocal_interval_ms";

    public required double NutritionDecayPerMinute { get; init; }
    public required double EnergyDecayPerMinute { get; init; }
    public required double HappinessDecayPerMinute { get; init; }
    public required double HappinessRestGainPerMinute { get; init; }
    public required int TiredEnergyThreshold { get; init; }
    public required int SleepWakeEnergyThreshold { get; init; }
    public required int HungerThreshold { get; init; }
    public required int ThirstThreshold { get; init; }
    public required int CommandHappinessReward { get; init; }
    public required int ToyHappinessReward { get; init; }
    public required int BoredHappinessThreshold { get; init; }
    public required int WanderIdleMinMs { get; init; }
    public required int WanderIdleMaxMs { get; init; }
    public required int VocalIntervalMs { get; init; }

    /// <summary>The compiled defaults, used before the first read and whenever a key is unset.</summary>
    public static PetTuning FromDefaults(PetConfig config) =>
        new()
        {
            NutritionDecayPerMinute = config.NutritionDecayPerMinute,
            EnergyDecayPerMinute = config.EnergyDecayPerMinute,
            HappinessDecayPerMinute = config.HappinessDecayPerMinute,
            HappinessRestGainPerMinute = config.HappinessRestGainPerMinute,
            TiredEnergyThreshold = config.TiredEnergyThreshold,
            SleepWakeEnergyThreshold = config.SleepWakeEnergyThreshold,
            HungerThreshold = config.HungerThreshold,
            ThirstThreshold = config.ThirstThreshold,
            CommandHappinessReward = config.CommandHappinessReward,
            ToyHappinessReward = config.ToyHappinessReward,
            BoredHappinessThreshold = config.BoredHappinessThreshold,
            WanderIdleMinMs = config.WanderIdleMinMs,
            WanderIdleMaxMs = config.WanderIdleMaxMs,
            VocalIntervalMs = config.VocalIntervalMs,
        };

    public static async Task<PetTuning> LoadAsync(IServerConfigGrain config, PetConfig defaults) =>
        new()
        {
            NutritionDecayPerMinute = await config
                .GetDoubleAsync(NutritionDecayPerMinuteKey, defaults.NutritionDecayPerMinute)
                .ConfigureAwait(false),
            EnergyDecayPerMinute = await config
                .GetDoubleAsync(EnergyDecayPerMinuteKey, defaults.EnergyDecayPerMinute)
                .ConfigureAwait(false),
            HappinessDecayPerMinute = await config
                .GetDoubleAsync(HappinessDecayPerMinuteKey, defaults.HappinessDecayPerMinute)
                .ConfigureAwait(false),
            HappinessRestGainPerMinute = await config
                .GetDoubleAsync(HappinessRestGainPerMinuteKey, defaults.HappinessRestGainPerMinute)
                .ConfigureAwait(false),
            TiredEnergyThreshold = await config
                .GetIntAsync(TiredEnergyThresholdKey, defaults.TiredEnergyThreshold)
                .ConfigureAwait(false),
            SleepWakeEnergyThreshold = await config
                .GetIntAsync(SleepWakeEnergyThresholdKey, defaults.SleepWakeEnergyThreshold)
                .ConfigureAwait(false),
            HungerThreshold = await config
                .GetIntAsync(HungerThresholdKey, defaults.HungerThreshold)
                .ConfigureAwait(false),
            ThirstThreshold = await config
                .GetIntAsync(ThirstThresholdKey, defaults.ThirstThreshold)
                .ConfigureAwait(false),
            CommandHappinessReward = await config
                .GetIntAsync(CommandHappinessRewardKey, defaults.CommandHappinessReward)
                .ConfigureAwait(false),
            ToyHappinessReward = await config
                .GetIntAsync(ToyHappinessRewardKey, defaults.ToyHappinessReward)
                .ConfigureAwait(false),
            BoredHappinessThreshold = await config
                .GetIntAsync(BoredHappinessThresholdKey, defaults.BoredHappinessThreshold)
                .ConfigureAwait(false),
            WanderIdleMinMs = await config
                .GetIntAsync(WanderIdleMinMsKey, defaults.WanderIdleMinMs)
                .ConfigureAwait(false),
            WanderIdleMaxMs = await config
                .GetIntAsync(WanderIdleMaxMsKey, defaults.WanderIdleMaxMs)
                .ConfigureAwait(false),
            VocalIntervalMs = await config
                .GetIntAsync(VocalIntervalMsKey, defaults.VocalIntervalMs)
                .ConfigureAwait(false),
        };
}
