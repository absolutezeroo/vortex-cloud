using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Vortex.Hosting.Tests.Architecture;
using Xunit;

namespace Vortex.Hosting.Tests;

/// <summary>
/// Every host module a project ships has to be named in <c>Program.cs</c>, because nothing else
/// looks for it.
/// </summary>
/// <remarks>
/// <para>
/// Host modules are registered from a hand-written list of <c>AddHostPlugin&lt;T&gt;</c> calls, not
/// discovered. A project can therefore be referenced by <c>Vortex.Main</c>, compile, ship, and have
/// its services simply never registered — and the whole solution still builds green, because the
/// only thing that notices is DI validation at startup.
/// </para>
/// <para>
/// That is not hypothetical: <c>SignalsModule</c> shipped without its line here, and the silo died
/// on boot with three unresolvable descriptors. It was loud only because two consumers happened to
/// inject one of its services. A module registering only handlers or hosted services would have
/// failed silently — the subsystem simply absent, with nothing in a log to say so.
/// </para>
/// </remarks>
public sealed class HostModuleRegistrationTests
{
    [Fact]
    public void Every_host_module_in_the_solution_is_registered_in_Program()
    {
        string root = RepositoryPaths.Root();
        string program = File.ReadAllText(Path.Combine(root, "Vortex.Main", "Program.cs"));

        HashSet<string> registered =
        [
            .. Regex
                .Matches(program, @"AddHostPlugin<(?<name>\w+)>")
                .Select(m => m.Groups["name"].Value),
        ];

        List<string> declared = [];

        foreach (
            string file in Directory.EnumerateFiles(root, "*Module.cs", SearchOption.AllDirectories)
        )
        {
            // bin/ and obj/ hold copies of generated sources; a match there is the same type twice.
            if (
                file.Contains(
                    $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal
                )
                || file.Contains(
                    $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal
                )
            )
            {
                continue;
            }

            foreach (
                Match match in Regex.Matches(
                    File.ReadAllText(file),
                    @"class\s+(?<name>\w+)\s*:\s*IHostPluginModule"
                )
            )
            {
                declared.Add(match.Groups["name"].Value);
            }
        }

        declared.Should().NotBeEmpty("the scan itself must be finding something");

        declared
            .Should()
            .OnlyContain(
                name => registered.Contains(name),
                "a host module absent from Program.cs registers nothing, and the build stays green"
            );
    }
}
