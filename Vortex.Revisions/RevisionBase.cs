using System;
using System.Collections.Generic;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions;

/// <summary>
/// Base implementation of <see cref="IRevision"/> that assembles its parser/serializer tables from
/// a set of <see cref="IRevisionMap"/> instances, one per message domain. A descendant revision that
/// only needs to change a handful of entries relative to this one can inherit from an existing
/// revision and override <see cref="ConfigureParsers"/>/<see cref="ConfigureSerializers"/> to add or
/// replace specific entries on top of whatever the maps already registered - without duplicating
/// every other entry.
/// </summary>
public abstract class RevisionBase : IRevision
{
    protected RevisionBase(IEnumerable<IRevisionMap> maps)
    {
        RevisionMapBuilder builder = new();

        foreach (IRevisionMap map in maps)
        {
            map.RegisterInto(builder);
        }

        ConfigureParsers(builder);
        ConfigureSerializers(builder);

        Parsers = builder.Parsers;
        Serializers = builder.Serializers;
    }

    public abstract string Revision { get; }

    public IReadOnlyDictionary<int, IParser> Parsers { get; }

    public IReadOnlyDictionary<Type, ISerializer> Serializers { get; }

    /// <summary>
    /// Override to add or replace specific parser entries after all maps have registered theirs.
    /// Runs once, during construction.
    /// </summary>
    protected virtual void ConfigureParsers(IRevisionMapBuilder builder) { }

    /// <summary>
    /// Override to add or replace specific serializer entries after all maps have registered theirs.
    /// Runs once, during construction.
    /// </summary>
    protected virtual void ConfigureSerializers(IRevisionMapBuilder builder) { }
}
