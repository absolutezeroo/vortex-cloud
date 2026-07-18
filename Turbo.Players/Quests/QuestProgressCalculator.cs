using System;

namespace Turbo.Players.Quests;

/// <summary>Pure quest objective-progress rule, extracted from the grain so it can be unit-tested.</summary>
public static class QuestProgressCalculator
{
    /// <summary>
    /// Adds <paramref name="amount"/> steps to <paramref name="currentSteps"/>, capped at
    /// <paramref name="totalSteps"/>. Returns the new step count and whether the goal is now reached.
    /// </summary>
    public static (int NewSteps, bool Completed) Apply(int currentSteps, int amount, int totalSteps)
    {
        int goal = Math.Max(1, totalSteps);
        int steps = Math.Clamp(currentSteps + amount, 0, goal);
        return (steps, steps >= goal);
    }
}
