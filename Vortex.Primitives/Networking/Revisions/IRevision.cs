using System;
using System.Collections.Generic;
using Vortex.Primitives.Packets;

namespace Vortex.Primitives.Networking.Revisions;

public interface IRevision
{
    public string Revision { get; }

    public IDictionary<int, IParser> Parsers { get; }

    public IDictionary<Type, ISerializer> Serializers { get; }
}
