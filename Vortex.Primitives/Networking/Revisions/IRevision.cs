using System;
using System.Collections.Generic;
using Vortex.Primitives.Packets;

namespace Vortex.Primitives.Networking.Revisions;

public interface IRevision
{
    public string Revision { get; }

    public IReadOnlyDictionary<int, IParser> Parsers { get; }

    public IReadOnlyDictionary<Type, ISerializer> Serializers { get; }
}
