using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Vortex.Hosting.Tests.Architecture;

/// <summary>
/// The room's concurrency model is Orleans and nothing else: one activation, one turn at a time, no
/// second synchronisation primitive layered on top. Every reference emulator this project was
/// compared against rebuilds that guarantee by hand — a dedicated thread and spin locks, a thread
/// manager injected into the wired pipeline — and inherits the bugs that come with it.
/// <para>
/// The risk here is regressive rather than novel: a <c>Task.Run</c> added "temporarily" during an
/// extraction to make a signature fit, and never taken out. These two tests are what makes that a
/// build failure instead of a code review nobody ran.
/// </para>
/// </summary>
public sealed class RoomGrainConcurrencyTests
{
    [Fact]
    public void RoomGrain_IsNotReentrant()
    {
        Type roomGrain = VortexTypes
            .All()
            .Single(t => t.Name == "RoomGrain" && t.Namespace == "Vortex.Rooms.Grains");

        roomGrain
            .GetCustomAttributes(inherit: true)
            .Select(a => a.GetType().Name)
            .Should()
            .NotContain(
                "ReentrantAttribute",
                "RoomGrain owns the room's mutable state; interleaving its turns would let a tick "
                    + "step observe another call's half-applied mutation. Interleaving is opted into "
                    + "per method and listed in the manifest, never granted wholesale"
            );
    }

    [Fact]
    public void RoomsProject_AddsNoSynchronisationOfItsOwn()
    {
        // Orleans already serialises the turn. A second primitive on top of it is either redundant
        // or a deadlock waiting for the room tick. `lock (` needs the word boundary: the room is
        // full of prose about a movement lock, a clock and an ice block.
        string[] forbidden =
        [
            "SemaphoreSlim",
            "Task.Run(",
            " lock (",
            "Monitor.Enter",
            "new Thread(",
        ];

        List<string> offenders = [];

        foreach (string file in SourceFiles(Path.Combine(RepositoryPaths.Root(), "Vortex.Rooms")))
        {
            foreach (string raw in File.ReadAllLines(file))
            {
                string line = raw.TrimStart();

                if (line.StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (string token in forbidden)
                {
                    if (raw.Contains(token, StringComparison.Ordinal))
                    {
                        offenders.Add($"{Path.GetFileName(file)}: {token.Trim()}");
                    }
                }
            }
        }

        offenders
            .Should()
            .BeEmpty(
                "the actor boundary is the synchronisation; a lock inside a grain fights the model "
                    + "it runs on"
            );
    }

    internal static IEnumerable<string> SourceFiles(string directory) =>
        Directory
            .EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(f =>
                !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
            );
}
