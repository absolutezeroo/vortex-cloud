using System;

namespace Vortex.Fishing;

/// <summary>
/// The Hook Havoc minigame, as both ends run it.
/// </summary>
/// <remarks>
/// <para>
/// Reconstructed from Habbo Origins, which has no client dump — the mechanic is what the guides
/// describe (Q nudges the line left, E right, keep the needle centred while a green bar fills before
/// time runs out, and <em>tap rather than hold, because holding overbalances the line</em>). The
/// arithmetic below is Vortex's, because no capture of Origins' exists. See the client's
/// <c>docs/vortex-original/fishing.md</c> §4.
/// </para>
/// <para>
/// <strong>The client plays it and this replays it.</strong> A minigame this tight is unplayable if
/// the server streams the needle back at any real latency, and a client that simply reported "I won"
/// is trivially faked — with a Golden Fish and bonus tokens on the line, neither end can own the
/// verdict alone. So the server issues a seed, the client plays against it, the whole input timeline
/// comes back, and this decides.
/// </para>
/// <para>
/// <strong>Every number and every operation here is part of the wire contract.</strong> The client
/// has to reproduce this exactly — the same generator, the same tick length, the same order of
/// operations — or a fair attempt is scored as a loss. Change one and both ends change together.
/// </para>
/// </remarks>
internal static class HookHavocSimulation
{
    /// <summary>How long one simulated step lasts. Ticks are the unit an input names.</summary>
    internal const int TickMs = 100;

    /// <summary>Hundredths of a percent. The bar is full at 100.00%.</summary>
    internal const int FullBar = 10000;

    /// <summary>How far one tap moves the needle.</summary>
    private const int NudgeStrength = 6;

    /// <summary>
    /// A tap on the tick immediately after another moves the needle twice as far. This is the
    /// "do not hold" the guides warn about, expressed as arithmetic: holding a key produces
    /// consecutive ticks, and consecutive ticks overshoot.
    /// </summary>
    private const int OverbalanceMultiplier = 2;

    /// <summary>How far the current drifts each tick, before the player corrects for it.</summary>
    private const int MaxDriftPerTick = 3;

    /// <summary>The bar empties at half the rate it fills, so a slip costs less than it earns.</summary>
    private const int DrainDivisor = 2;

    /// <summary>
    /// Runs one attempt and answers whether the bar filled in time.
    /// </summary>
    /// <param name="timeline">
    /// Flat pairs of tick then direction — -1 for Q, +1 for E. An odd-length list is malformed and
    /// its trailing value is ignored; a direction that is neither -1 nor +1 is ignored too. Neither
    /// is treated as cheating, because neither gains anything: an ignored input is a wasted tap.
    /// </param>
    internal static bool Replay(
        int[] timeline,
        int seed,
        int durationMs,
        int fillRate,
        int tolerance
    )
    {
        int totalTicks = Math.Max(1, durationMs / TickMs);
        Xorshift32 rng = new(seed);

        int needle = 0;
        int fill = 0;
        int inputIndex = 0;
        int previousInputTick = int.MinValue;

        for (int tick = 0; tick < totalTicks; tick++)
        {
            // The drift is drawn every tick whether or not the player acted, so the generator stays
            // in step between the two ends no matter what was typed.
            needle += rng.Next(-MaxDriftPerTick, MaxDriftPerTick);

            // A timeline arrives in tick order; anything out of order or past the end is skipped
            // rather than rejected, because a reordered list is what a lossy client produces and it
            // cannot help the player.
            while (inputIndex + 1 < timeline.Length && timeline[inputIndex] <= tick)
            {
                int inputTick = timeline[inputIndex];
                int direction = timeline[inputIndex + 1];

                inputIndex += 2;

                if (inputTick != tick || (direction != -1 && direction != 1))
                {
                    continue;
                }

                int strength =
                    inputTick == previousInputTick + 1
                        ? NudgeStrength * OverbalanceMultiplier
                        : NudgeStrength;

                needle += direction * strength;
                previousInputTick = inputTick;
            }

            fill += Math.Abs(needle) <= tolerance ? fillRate : -(fillRate / DrainDivisor);
            fill = Math.Clamp(fill, 0, FullBar);

            if (fill >= FullBar)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The 32-bit xorshift both ends run.
    /// </summary>
    /// <remarks>
    /// Not <see cref="Random"/>: its sequence is an implementation detail of the runtime and is not
    /// reproducible in a browser, which makes it useless for anything the client also has to
    /// compute. Xorshift is four operations and identical everywhere — the same reason the snow-war
    /// port carries its own.
    /// </remarks>
    private struct Xorshift32(int seed)
    {
        // Zero is xorshift's fixed point: seeded with it the generator returns zero forever, and the
        // needle would never drift. Any non-zero substitute will do.
        private uint _state = seed == 0 ? 2463534242u : (uint)seed;

        /// <summary>A value in [min, max], both ends included.</summary>
        internal int Next(int min, int max)
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;

            return min + (int)(_state % (uint)(max - min + 1));
        }
    }
}
