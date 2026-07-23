using System;
using System.Text.Json;

namespace Vortex.Rooms.Wired;

/// <summary>
/// Rematerializes the type-erased specific slots of <see cref="WiredData"/>
/// (<c>DefinitionSpecifics</c> / <c>TypeSpecifics</c>, both <c>List&lt;object&gt;</c>).
/// <para>
/// At runtime the slots hold real CLR values (int / byte / bool) coming from the revision parser,
/// but after a persistence round-trip through the furni's <c>extra_data</c> JSON every slot comes
/// back as a <see cref="JsonElement"/> — a plain <c>IsInstanceOfType</c> check then fails and the
/// player's configuration (action delays, condition invert/quantifier flags, selector filter
/// flags) would silently reset to defaults on room reload. This codec bridges that gap: a value
/// already of the declared slot type passes through, a <see cref="JsonElement"/> is deserialized
/// to the declared type, anything else is rejected so the caller can fall back loudly.
/// </para>
/// </summary>
public static class WiredSpecificsCodec
{
    public static bool TryMaterialize(object? stored, Type slotType, out object? value)
    {
        if (stored is not null && slotType.IsInstanceOfType(stored))
        {
            value = stored;

            return true;
        }

        if (stored is JsonElement element)
        {
            try
            {
                value = element.Deserialize(slotType);

                return value is not null;
            }
            catch (Exception)
            {
                // Mismatched kind (e.g. a string where an int is declared) or an out-of-range
                // number for the target type: the slot is unusable, let the caller fall back.
                value = null;

                return false;
            }
        }

        value = null;

        return false;
    }
}
