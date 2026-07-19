using System.Collections.Immutable;

namespace Vortex.Primitives.Orleans.Snapshots.Room;

/// <summary>Shared Tag1/Tag2 (RoomEntity) -&gt; Tags (RoomInfoSnapshot) mapping so navigator search
/// results and the live room snapshot never drift on how blank/whitespace tags are handled.</summary>
public static class RoomTagMapper
{
    public static ImmutableArray<string> ToTags(string? tag1, string? tag2)
    {
        ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>(2);

        if (!string.IsNullOrWhiteSpace(tag1))
        {
            builder.Add(tag1.Trim());
        }

        if (!string.IsNullOrWhiteSpace(tag2))
        {
            builder.Add(tag2.Trim());
        }

        return builder.ToImmutable();
    }
}
