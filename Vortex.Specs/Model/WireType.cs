namespace Vortex.Specs.Model;

/// <summary>
/// The primitive types the Habbo wire format actually carries. Anything an implementation models as
/// a richer type (an enum, a decimal encoded as text) still travels as one of these, and the spec
/// records what travels rather than what the implementation calls it.
/// </summary>
public enum WireType
{
    Unknown = 0,
    Int32,
    Boolean,
    String,
    Byte,
    Short,
    Long,
    Float,
    Double,

    /// <summary>
    /// A repeated block whose length is carried by a preceding int32. The block's own layout lives
    /// in <see cref="PacketFieldSpec.Children"/>.
    /// </summary>
    Array,

    /// <summary>
    /// A named group of fields written inline with no length prefix — a shared sub-serializer such
    /// as the floor-item block. Its layout lives in <see cref="PacketFieldSpec.Children"/>.
    /// </summary>
    Block,
}

public static class WireTypeNames
{
    public static string Wire(this WireType type) =>
        type switch
        {
            WireType.Int32 => "int32",
            WireType.Boolean => "boolean",
            WireType.String => "string",
            WireType.Byte => "byte",
            WireType.Short => "short",
            WireType.Long => "long",
            WireType.Float => "float",
            WireType.Double => "double",
            WireType.Array => "array",
            WireType.Block => "block",
            _ => "unknown",
        };

    public static WireType Parse(string text) =>
        text switch
        {
            "int32" => WireType.Int32,
            "boolean" => WireType.Boolean,
            "string" => WireType.String,
            "byte" => WireType.Byte,
            "short" => WireType.Short,
            "long" => WireType.Long,
            "float" => WireType.Float,
            "double" => WireType.Double,
            "array" => WireType.Array,
            "block" => WireType.Block,
            _ => WireType.Unknown,
        };
}
