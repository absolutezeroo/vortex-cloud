using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Vortex.Hosting.Tests.Architecture;

/// <summary>
/// A configured budget with no runtime reader is worse than a hardcoded one: the hardcoded value is
/// at least honest about being the value. <c>RoomConfig.WiredMaxDepth</c> said 20 for as long as it
/// existed; the wired engine enforced a private const of 8, and an operator raising the setting
/// changed nothing at all (RFW-101).
/// <para>
/// This test is the ratchet. Every <c>Wired*</c> knob on <c>RoomConfig</c> must be read somewhere
/// outside the configuration class itself, so the next one cannot be added and forgotten.
/// </para>
/// </summary>
public sealed class ConfiguredBudgetTests
{
    [Fact]
    public void EveryWiredBudget_HasARuntimeReader()
    {
        Type roomConfig = VortexTypes
            .All()
            .Single(t => t.Name == "RoomConfig" && t.Namespace == "Vortex.Rooms.Configuration");

        string[] knobs =
        [
            .. roomConfig
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .Where(n => n.StartsWith("Wired", StringComparison.Ordinal)),
        ];

        knobs.Should().NotBeEmpty("the configuration class is the one being guarded");

        string configFile = Path.Combine(
            RepositoryPaths.Root(),
            "Vortex.Rooms",
            "Configuration",
            "RoomConfig.cs"
        );

        HashSet<string> read = [];

        foreach (
            string file in RoomGrainConcurrencyTests.SourceFiles(
                Path.Combine(RepositoryPaths.Root(), "Vortex.Rooms")
            )
        )
        {
            if (string.Equals(file, configFile, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string source = File.ReadAllText(file);

            foreach (string knob in knobs)
            {
                if (source.Contains(knob, StringComparison.Ordinal))
                {
                    read.Add(knob);
                }
            }
        }

        knobs
            .Except(read)
            .Should()
            .BeEmpty(
                "a wired budget nothing reads is a setting that lies to whoever changes it — wire it "
                    + "to the code that enforces it, or delete it"
            );
    }
}
