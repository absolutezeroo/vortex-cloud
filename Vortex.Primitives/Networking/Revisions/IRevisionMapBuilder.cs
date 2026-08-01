using System;
using Vortex.Primitives.Packets;

namespace Vortex.Primitives.Networking.Revisions;

public interface IRevisionMapBuilder
{
    void MapParser(int header, IParser parser);

    void MapSerializer(Type composerType, ISerializer serializer);

    void MapSerializer<TComposer>(ISerializer serializer) =>
        MapSerializer(typeof(TComposer), serializer);
}
