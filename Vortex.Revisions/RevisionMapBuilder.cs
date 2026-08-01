using System;
using System.Collections.Generic;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions;

/// <summary>
/// Mutable collector used while a revision is being assembled from its <see cref="IRevisionMap"/>
/// instances. Frozen (exposed as read-only) once <see cref="RevisionBase"/> is done building it.
/// </summary>
internal sealed class RevisionMapBuilder : IRevisionMapBuilder
{
    private readonly Dictionary<int, IParser> _parsers = new();
    private readonly Dictionary<Type, ISerializer> _serializers = new();

    public IReadOnlyDictionary<int, IParser> Parsers => _parsers;

    public IReadOnlyDictionary<Type, ISerializer> Serializers => _serializers;

    public void MapParser(int header, IParser parser)
    {
        if (!_parsers.TryAdd(header, parser))
        {
            throw new InvalidOperationException(
                $"Header {header} is already mapped to a parser - two maps registered the same "
                    + "header instead of one being deleted or renamed."
            );
        }
    }

    public void MapSerializer(Type composerType, ISerializer serializer)
    {
        if (!_serializers.TryAdd(composerType, serializer))
        {
            throw new InvalidOperationException(
                $"Composer type {composerType} is already mapped to a serializer - two maps "
                    + "registered the same type instead of one being deleted or renamed."
            );
        }
    }
}
