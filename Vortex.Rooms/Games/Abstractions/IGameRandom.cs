using System;
using System.Collections.Generic;

namespace Vortex.Rooms.Games.Abstractions;

/// <summary>
/// The only source of randomness a game rule may use. It exists so that a match is reproducible: the
/// runtime seeds one of these per match from the match id, so the same inputs replay to the same
/// power-ups and the same teleport destinations, and a test can assert on either without stubbing
/// <c>Random.Shared</c> — which cannot be stubbed at all.
/// </summary>
public interface IGameRandom
{
    /// <summary>A non-negative integer below <paramref name="exclusiveMax"/>. 0 for a non-positive
    /// bound, never a throw: a roll over an empty candidate list is a normal state in a room whose
    /// furniture was just picked up.</summary>
    int Next(int exclusiveMax);

    /// <summary>True with <paramref name="percent"/> percent probability.</summary>
    bool Chance(int percent);

    /// <summary>A random element, or <c>default</c> for an empty list.</summary>
    T? Pick<T>(IReadOnlyList<T> candidates);
}

/// <summary>The real one: a <see cref="Random"/> seeded per match.</summary>
public sealed class GameRandom(int seed) : IGameRandom
{
    private readonly Random _random = new(seed);

    public int Next(int exclusiveMax) => exclusiveMax <= 0 ? 0 : _random.Next(exclusiveMax);

    public bool Chance(int percent) => percent > 0 && _random.Next(100) < percent;

    public T? Pick<T>(IReadOnlyList<T> candidates) =>
        candidates.Count == 0 ? default : candidates[_random.Next(candidates.Count)];
}
