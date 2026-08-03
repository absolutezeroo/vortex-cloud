using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Vortex.Logging;
using Vortex.Primitives;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Furniture.Wall;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;
using Vortex.Primitives.Rooms.Providers;
using Vortex.Rooms.Object;
using Vortex.Rooms.Object.Logic;
using Vortex.Runtime;

namespace Vortex.Rooms.Providers;

public sealed class RoomObjectLogicProvider(
    IServiceProvider host,
    ILogger<RoomObjectLogicProvider> logger
) : IRoomObjectLogicProvider
{
    /// <summary>
    /// Which room-object family a logic can be constructed for.
    /// </summary>
    /// <remarks>
    /// A logic name is not unique on its own: the client uses <c>furniture_multistate</c> and
    /// <c>furniture_basic</c> for floor and wall furniture alike. The family has to be part of the
    /// key, or registering the name for one family silently replaces the other — and the mismatch
    /// only surfaces when someone places the furni, because the two contexts share no interface and
    /// the logic simply cannot be built.
    /// </remarks>
    private enum LogicFamily
    {
        Any,
        Floor,
        Wall,
    }

    private const string DefaultFloorLogic = "default_floor";
    private const string DefaultWallLogic = "default_wall";

    private readonly IServiceProvider _host = host;
    private readonly ILogger<RoomObjectLogicProvider> _logger = logger;
    private readonly ConcurrentDictionary<
        (string Name, LogicFamily Family),
        RoomObjectLogicReg
    > _logics = [];

    public IDisposable RegisterLogic(
        string logicType,
        Type implementation,
        IServiceProvider sp,
        Func<IServiceProvider, IRoomObjectContext, IRoomObjectLogic> factory
    )
    {
        RoomObjectLogicReg reg = new RoomObjectLogicReg(sp, factory);
        (string, LogicFamily) key = (logicType, FamilyOf(implementation));

        _logics[key] = reg;

        return new ActionDisposable(() =>
        {
            _logics.TryRemove(
                new KeyValuePair<(string, LogicFamily), RoomObjectLogicReg>(key, reg)
            );
        });
    }

    public IRoomObjectLogic CreateLogicInstance(string logicType, IRoomObjectContext ctx)
    {
        LogicFamily family = FamilyOf(ctx);
        RoomObjectLogicReg? reg = Resolve(logicType, family);

        if (reg is null)
        {
            // Not an error on its own: the catalogue carries client logic names for behaviour Vortex
            // has not implemented yet, and the family default is the correct stand-in. The warning
            // is the to-do list — it names exactly which logic is missing.
            string fallback = family is LogicFamily.Wall ? DefaultWallLogic : DefaultFloorLogic;

            _logger.LogWarning(
                "Logic type '{LogicType}' not found for {Family} objects, falling back to {Fallback}",
                logicType,
                family,
                fallback
            );

            reg = Resolve(fallback, family);
        }

        if (reg is null)
        {
            throw new VortexException(VortexErrorCodeEnum.InvalidLogic);
        }

        IServiceProvider sp = reg.ServiceProvider;

        if (sp != _host)
        {
            sp = new CompositeServiceProvider(sp, _host);
        }

        IRoomObjectLogic logic = reg.Factory(sp, ctx);

        if (logic is RoomObjectLogicBase withLogger)
        {
            withLogger.AttachLogger(
                sp.GetService(typeof(ILoggerFactory)) is ILoggerFactory factory
                    ? factory.CreateLogger(logic.GetType())
                    : _logger
            );
        }

        return logic;
    }

    /// <summary>
    /// Prefers the registration made for this exact family, then one that fits any family (avatars,
    /// and logics that take the plain context). A floor logic is never handed a wall context.
    /// </summary>
    private RoomObjectLogicReg? Resolve(string logicType, LogicFamily family)
    {
        if (_logics.TryGetValue((logicType, family), out RoomObjectLogicReg? exact))
        {
            return exact;
        }

        return _logics.TryGetValue((logicType, LogicFamily.Any), out RoomObjectLogicReg? any)
            ? any
            : null;
    }

    private static LogicFamily FamilyOf(Type implementation)
    {
        if (typeof(IFurnitureWallLogic).IsAssignableFrom(implementation))
        {
            return LogicFamily.Wall;
        }

        return typeof(IFurnitureFloorLogic).IsAssignableFrom(implementation)
            ? LogicFamily.Floor
            : LogicFamily.Any;
    }

    private static LogicFamily FamilyOf(IRoomObjectContext ctx) =>
        ctx switch
        {
            IRoomWallItemContext => LogicFamily.Wall,
            IRoomFloorItemContext => LogicFamily.Floor,
            _ => LogicFamily.Any,
        };
}
