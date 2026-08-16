using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace Vortex.Primitives.Benchmark;

/// <summary>How a run should be read.</summary>
public enum BenchmarkGrade
{
    /// <summary>Nothing a player would have noticed.</summary>
    Good,

    /// <summary>Working, with headroom running out. Worth watching, not worth panicking over.</summary>
    Watch,

    /// <summary>A player in that room had a bad time.</summary>
    Bad,
}

/// <summary>
/// The verdict on a run, and the reason for it.
/// </summary>
/// <remarks>
/// A page full of milliseconds tells you nothing unless you already know what good looks like, which
/// is precisely the knowledge somebody running their first load test does not have. So the judgement
/// is made once, here, in terms of what a player would have felt — and the thresholds are written
/// down with their reasons rather than tuned until the graph looked nice.
/// </remarks>
public sealed record BenchmarkVerdict
{
    public required BenchmarkGrade Grade { get; init; }

    /// <summary>One sentence, in plain terms, saying what happened.</summary>
    public required string Headline { get; init; }

    /// <summary>The observations behind the grade, worst first.</summary>
    public required ImmutableArray<string> Findings { get; init; }

    public required double MedianRttMs { get; init; }

    public required int Stalls { get; init; }

    /// <summary>
    /// The slowest tick as a share of what a tick is allowed to take. A room ticks twenty times a
    /// second, so a tick has 50 ms before it is late — and a late tick is an avatar that does not
    /// move when the player says so.
    /// </summary>
    public required double TickBudgetPercent { get; init; }

    private const double TickBudgetMs = 50;

    /// <summary>A round trip a player would notice as a freeze rather than as lag.</summary>
    private const double StallMs = 1000;

    /// <summary>Above this the hotel feels sluggish to everyone in the room, all the time.</summary>
    private const double SluggishMedianMs = 100;

    private const double UncomfortableMedianMs = 30;

    public static BenchmarkVerdict Evaluate(
        ImmutableArray<BenchmarkSample> samples,
        double tickP99Ms
    )
    {
        if (samples.IsEmpty)
        {
            return new BenchmarkVerdict
            {
                Grade = BenchmarkGrade.Watch,
                Headline = "No samples were collected, so this run says nothing either way.",
                Findings = [],
                MedianRttMs = 0,
                Stalls = 0,
                TickBudgetPercent = 0,
            };
        }

        // The ramp is excluded: players arriving is a burst by construction, and judging the hotel
        // on the second it was accepting fifty logins would condemn every run.
        ImmutableArray<BenchmarkSample> steady =
        [
            .. samples.Skip(Math.Min(samples.Length / 3, samples.Length - 1)),
        ];

        double[] medians = [.. steady.Where(s => s.RttMedianMs > 0).Select(s => s.RttMedianMs)];

        Array.Sort(medians);

        double median = medians.Length == 0 ? 0 : medians[medians.Length / 2];
        int stalls = steady.Count(s => s.RttP95Ms >= StallMs);
        long failures = samples[^1].Failures;
        double budget = Math.Round(tickP99Ms / TickBudgetMs * 100, 1);

        ImmutableArray<string>.Builder findings = ImmutableArray.CreateBuilder<string>();
        BenchmarkGrade grade = BenchmarkGrade.Good;

        if (failures > 0)
        {
            grade = BenchmarkGrade.Bad;
            findings.Add(
                Line($"{failures} connection(s) dropped or failed to send. Sockets are being lost.")
            );
        }

        if (stalls > 0)
        {
            // A stall is the one thing here a player cannot fail to notice, so it outranks averages
            // however good they are.
            grade = stalls > 1 ? BenchmarkGrade.Bad : Worst(grade, BenchmarkGrade.Watch);
            findings.Add(
                Line(
                    $"{stalls} second(s) where the slowest round trip passed one second — the room froze for somebody."
                )
            );
        }

        if (median >= SluggishMedianMs)
        {
            grade = BenchmarkGrade.Bad;
            findings.Add(Line($"Typical round trip {median:F0} ms. Everything feels heavy."));
        }
        else if (median >= UncomfortableMedianMs)
        {
            grade = Worst(grade, BenchmarkGrade.Watch);
            findings.Add(Line($"Typical round trip {median:F0} ms — noticeable, not yet painful."));
        }

        if (budget >= 100)
        {
            grade = BenchmarkGrade.Bad;
            findings.Add(
                Line(
                    $"The slowest room tick used {budget:F0}% of its 50 ms budget: ticks are late."
                )
            );
        }
        else if (budget >= 50)
        {
            grade = Worst(grade, BenchmarkGrade.Watch);
            findings.Add(
                Line(
                    $"The slowest room tick used {budget:F0}% of its 50 ms budget. Headroom is thin."
                )
            );
        }

        string headline = grade switch
        {
            BenchmarkGrade.Good => Line(
                $"Comfortable. Typical round trip {median:F1} ms, no freezes, ticks at {budget:F0}% of budget."
            ),
            BenchmarkGrade.Watch => Line(
                $"Holding, with less room than it looks. Typical round trip {median:F1} ms."
            ),
            _ => Line($"A player in that room had a bad time. Typical round trip {median:F1} ms."),
        };

        return new BenchmarkVerdict
        {
            Grade = grade,
            Headline = headline,
            Findings = findings.ToImmutable(),
            MedianRttMs = Math.Round(median, 2),
            Stalls = stalls,
            TickBudgetPercent = budget,
        };
    }

    private static BenchmarkGrade Worst(BenchmarkGrade a, BenchmarkGrade b) =>
        (BenchmarkGrade)Math.Max((int)a, (int)b);

    private static string Line(FormattableString text) =>
        text.ToString(CultureInfo.InvariantCulture);
}
