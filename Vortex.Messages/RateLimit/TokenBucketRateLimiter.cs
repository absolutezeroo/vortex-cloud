using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Options;
using Vortex.Messages.Configuration;
using Vortex.Primitives.Networking;

namespace Vortex.Messages.RateLimit;

/// <summary>
/// One token bucket per session, refilled continuously at <see cref="RateLimitConfig.MaxPacketsPerSecond"/>
/// up to <see cref="RateLimitConfig.BurstSize"/>. Registered as a singleton: the buckets must
/// outlive any single packet's dispatch, since <c>RateLimitBehavior</c> itself is constructed fresh
/// per invocation by the pipeline.
/// </summary>
public sealed class TokenBucketRateLimiter : IRateLimiter
{
    // Swept every this-many calls rather than on a timer, so an idle process spends nothing.
    private const int SweepEveryNCalls = 4096;
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<SessionKey, Bucket> _buckets = new();
    private readonly double _ratePerSecond;
    private readonly double _capacity;
    private long _callCounter;

    public TokenBucketRateLimiter(IOptions<RateLimitConfig> options)
    {
        RateLimitConfig config = options.Value;
        _ratePerSecond = Math.Max(1, config.MaxPacketsPerSecond);
        _capacity = Math.Max(_ratePerSecond, config.BurstSize);
    }

    public bool TryConsume(SessionKey session)
    {
        Bucket bucket = _buckets.GetOrAdd(session, _ => new Bucket(_capacity));

        bool allowed;

        lock (bucket)
        {
            long now = Stopwatch.GetTimestamp();
            double elapsedSeconds = Stopwatch.GetElapsedTime(bucket.LastRefillTimestamp, now)
                .TotalSeconds;

            if (elapsedSeconds > 0)
            {
                bucket.Tokens = Math.Min(_capacity, bucket.Tokens + (elapsedSeconds * _ratePerSecond));
                bucket.LastRefillTimestamp = now;
            }

            bucket.LastTouchedUtc = DateTime.UtcNow;

            if (bucket.Tokens >= 1)
            {
                bucket.Tokens -= 1;
                allowed = true;
            }
            else
            {
                allowed = false;
            }
        }

        if (Interlocked.Increment(ref _callCounter) % SweepEveryNCalls == 0)
        {
            SweepStaleBuckets();
        }

        return allowed;
    }

    private void SweepStaleBuckets()
    {
        DateTime cutoff = DateTime.UtcNow - StaleAfter;

        foreach ((SessionKey key, Bucket bucket) in _buckets)
        {
            bool stale;

            lock (bucket)
            {
                stale = bucket.LastTouchedUtc < cutoff;
            }

            if (stale)
            {
                _buckets.TryRemove(key, out _);
            }
        }
    }

    private sealed class Bucket(double initialTokens)
    {
        public double Tokens = initialTokens;
        public long LastRefillTimestamp = Stopwatch.GetTimestamp();
        public DateTime LastTouchedUtc = DateTime.UtcNow;
    }
}
