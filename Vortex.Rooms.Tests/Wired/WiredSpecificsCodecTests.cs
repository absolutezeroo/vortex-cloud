using System;
using System.Text.Json;
using FluentAssertions;
using Vortex.Rooms.Wired;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// Locks the persistence round-trip of the type-erased specific slots
/// (<see cref="WiredData.DefinitionSpecifics"/> / <see cref="WiredData.TypeSpecifics"/>). These are
/// <c>List&lt;object&gt;</c>: at runtime they hold real CLR values, but after the extra_data JSON
/// round-trip every slot comes back as a <see cref="JsonElement"/>. The codec must recover the
/// declared type in that case — otherwise action delays, condition invert/quantifier flags and
/// selector filter flags silently reset to defaults on every room reload.
/// </summary>
public sealed class WiredSpecificsCodecTests
{
    [Fact]
    public void TypedValues_PassThrough_Unchanged()
    {
        WiredSpecificsCodec.TryMaterialize(7, typeof(int), out object? intValue).Should().BeTrue();
        intValue.Should().Be(7);

        WiredSpecificsCodec
            .TryMaterialize((byte)3, typeof(byte), out object? byteValue)
            .Should()
            .BeTrue();
        byteValue.Should().Be((byte)3);

        WiredSpecificsCodec
            .TryMaterialize(true, typeof(bool), out object? boolValue)
            .Should()
            .BeTrue();
        boolValue.Should().Be(true);
    }

    [Fact]
    public void JsonRoundTrip_Of_WiredData_Recovers_TypedSlots()
    {
        // Exactly the persistence path of FurnitureWiredLogic: SerializeToNode into the extra_data
        // section, then Deserialize<WiredData> on rehydration.
        WiredData data = new() { DefinitionSpecifics = [7], TypeSpecifics = [(byte)3, true] };

        string persisted = JsonSerializer.SerializeToNode(data, data.GetType())!.ToJsonString();
        WiredData reloaded = JsonSerializer.Deserialize<WiredData>(persisted)!;

        // Sanity: without the codec the slots are JsonElements, not the declared types.
        reloaded.DefinitionSpecifics[0].Should().BeOfType<JsonElement>();

        WiredSpecificsCodec
            .TryMaterialize(reloaded.DefinitionSpecifics[0], typeof(int), out object? delay)
            .Should()
            .BeTrue();
        delay.Should().Be(7);

        WiredSpecificsCodec
            .TryMaterialize(reloaded.TypeSpecifics[0], typeof(byte), out object? quantifier)
            .Should()
            .BeTrue();
        quantifier.Should().Be((byte)3);

        WiredSpecificsCodec
            .TryMaterialize(reloaded.TypeSpecifics[1], typeof(bool), out object? invert)
            .Should()
            .BeTrue();
        invert.Should().Be(true);
    }

    [Fact]
    public void MismatchedJsonKind_IsRejected_NotCoerced()
    {
        JsonElement stringElement = JsonSerializer.SerializeToElement("not-a-number");

        WiredSpecificsCodec
            .TryMaterialize(stringElement, typeof(int), out object? value)
            .Should()
            .BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void OutOfRangeNumber_ForByteSlot_IsRejected()
    {
        JsonElement tooBig = JsonSerializer.SerializeToElement(300);

        WiredSpecificsCodec
            .TryMaterialize(tooBig, typeof(byte), out object? value)
            .Should()
            .BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void NullSlot_IsRejected()
    {
        WiredSpecificsCodec.TryMaterialize(null, typeof(int), out object? value).Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void WrongClrType_ForSlot_IsRejected()
    {
        // A boxed int in a byte slot must not be silently unboxed to the wrong type.
        WiredSpecificsCodec.TryMaterialize(7, typeof(byte), out object? value).Should().BeFalse();
        value.Should().BeNull();
    }
}
