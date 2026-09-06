using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Vortex.Primitives.Signals;
using Vortex.Signals.Translators;
using Xunit;

namespace Vortex.Signals.Tests;

/// <summary>
/// The one test that must not go through discovery.
/// </summary>
/// <remarks>
/// <para>
/// <c>AssemblyExplorer</c> keeps public types only. A translator written without <c>public</c> is
/// therefore never discovered, never loaded, and never refused by the processor's own check — and
/// every other test in this project also discovers through that path, so none of them would see it
/// either. The action it produces would simply go silent, with no exception and nothing in a log.
/// </para>
/// <para>
/// So this sweeps the assembly by reflection, non-public types included, and compares what exists
/// against what the runtime would actually register.
/// </para>
/// </remarks>
public sealed class TranslatorVisibilityTests
{
    [Fact]
    public void Every_translator_in_the_assembly_is_public()
    {
        ImmutableArray<string> invisible =
        [
            .. typeof(RoomEntryTranslator)
                .Assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericTypeDefinition)
                .Where(t => !t.IsPublic && Implements(t))
                .Select(t => t.FullName ?? t.Name),
        ];

        invisible
            .Should()
            .BeEmpty(
                "a non-public translator compiles, ships, is never registered, and makes its "
                    + "action silent without raising anything"
            );
    }

    [Fact]
    public void Every_translator_can_be_built_without_arguments()
    {
        // The processor builds one instance per translator and holds it for the lifetime of the
        // silo. A constructor parameter would mean either an activation per walked tile or a
        // dependency resolved from a plugin's provider, which does not see host services.
        foreach (TranslatorUnderTest translator in TranslatorCatalog.All)
        {
            translator
                .Concrete.GetConstructor(Type.EmptyTypes)
                .Should()
                .NotBeNull($"{translator.Name} must be a pure function with nothing injected");
        }
    }

    [Fact]
    public void Every_translator_declares_at_least_one_shape()
    {
        foreach (TranslatorUnderTest translator in TranslatorCatalog.All)
        {
            translator
                .Shapes.Should()
                .NotBeEmpty(
                    $"nothing {translator.Name} produces could be filtered or offered in the editor"
                );
        }
    }

    private static bool Implements(Type type) =>
        type.GetInterfaces()
            .Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISignalTranslator<>)
            );
}
