using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Players;

namespace Vortex.Catalog.Vouchers;

/// <summary>
/// How many voucher codes one player may get wrong before being made to wait.
/// </summary>
/// <remarks>
/// <para>
/// A voucher code is a secret worth currency, and nothing counted how often one was guessed wrong
/// (SEC-11). The redemption checks themselves are sound -- already-redeemed, redemption ceiling,
/// player exists -- but they answer per code, and a guesser does not care about being told no.
/// What stops guessing is the cost of the next attempt.
/// </para>
/// <para>
/// Only FAILURES consume a token, so a player redeeming real codes one after another is never
/// slowed. This must be invisible to everyone except someone guessing.
/// </para>
/// </remarks>
public sealed class VoucherAttemptLimiter : IVoucherAttemptLimiter
{
    // Generous for a person typing a code off a card and fumbling it; hopeless for a search.
    private const int Capacity = 10;
    private const double RefillPerSecond = 1.0 / 60.0;
    private const int SweepEveryNCalls = 1024;
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<PlayerId, Bucket> _buckets = new();
    private long _callCounter;

    public bool MayAttempt(PlayerId playerId)
    {
        Bucket bucket = _buckets.GetOrAdd(playerId, static _ => new Bucket(Capacity));

        lock (bucket)
        {
            Refill(bucket);

            return bucket.Tokens >= 1;
        }
    }

    public void RecordFailure(PlayerId playerId)
    {
        Bucket bucket = _buckets.GetOrAdd(playerId, static _ => new Bucket(Capacity));

        lock (bucket)
        {
            Refill(bucket);

            if (bucket.Tokens >= 1)
            {
                bucket.Tokens -= 1;
            }
        }

        Sweep();
    }

    private static void Refill(Bucket bucket)
    {
        long now = Stopwatch.GetTimestamp();
        double elapsedSeconds = Stopwatch
            .GetElapsedTime(bucket.LastRefillTimestamp, now)
            .TotalSeconds;

        if (elapsedSeconds > 0)
        {
            bucket.Tokens = Math.Min(Capacity, bucket.Tokens + (elapsedSeconds * RefillPerSecond));
            bucket.LastRefillTimestamp = now;
        }

        bucket.LastTouchedUtc = DateTime.UtcNow;
    }

    // Swept on the way past rather than on a timer, so an idle process spends nothing -- the same
    // arrangement TokenBucketRateLimiter uses.
    private void Sweep()
    {
        if (Interlocked.Increment(ref _callCounter) % SweepEveryNCalls != 0)
        {
            return;
        }

        DateTime cutoff = DateTime.UtcNow - StaleAfter;

        foreach (KeyValuePair<PlayerId, Bucket> entry in _buckets)
        {
            if (entry.Value.LastTouchedUtc < cutoff)
            {
                _buckets.TryRemove(entry.Key, out _);
            }
        }
    }

    private sealed class Bucket(double tokens)
    {
        public double Tokens { get; set; } = tokens;
        public long LastRefillTimestamp { get; set; } = Stopwatch.GetTimestamp();
        public DateTime LastTouchedUtc { get; set; } = DateTime.UtcNow;
    }
}
