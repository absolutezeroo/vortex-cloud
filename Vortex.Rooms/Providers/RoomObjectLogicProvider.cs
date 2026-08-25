using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Vortex.Logging;
using Vortex.Primitives;
using Vortex.Primitives.Observability;
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
    IVortexMetrics metrics,
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
    private readonly IVortexMetrics _metrics = metrics;
    private readonly ILogger<RoomObjectLogicProvider> _logger = logger;
    private readonly ConcurrentDictionary<
        (string Name, LogicFamily Family),
        RoomObjectLogicReg
    > _logics = [];

    /// <summary>
    /// Registers a logic for a name and family, and returns the disposable that unregisters it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A collision fails the registration rather than winning it. The old behaviour was
    /// <c>_logics[key] = reg</c> — the last registration silently replaced whatever was there — and
    /// the disposable removed the key only if the removed registration was still the current one.
    /// So a plugin registering a name the core already owned took the furni over, and unloading that
    /// plugin left the core's furni on the family default: it fell back to <c>default_floor</c>,
    /// wrote a warning nobody was reading, and behaved like a plain item until the next restart.
    /// </para>
    /// <para>
    /// Failing at registration is louder and earlier, and the message names both implementations.
    /// A deliberate override is a feature nobody has asked for; when someone does, it is a stack of
    /// registrations with an explicit order that restores the previous one on dispose — not an
    /// overwrite that cannot be undone.
    /// </para>
    /// </remarks>
    /// <exception cref="VortexException">Another implementation already holds this name and family.</exception>
    public IDisposable RegisterLogic(
        string logicType,
        Type implementation,
        IServiceProvider sp,
        Func<IServiceProvider, IRoomObjectContext, IRoomObjectLogic> factory
    )
    {
        RoomObjectLogicReg reg = new RoomObjectLogicReg(sp, factory)
        {
            Implementation = implementation,
        };
        (string, LogicFamily) key = (logicType, FamilyOf(implementation));

        if (!_logics.TryAdd(key, reg))
        {
            RoomObjectLogicReg existing = _logics[key];

            if (existing.Implementation == implementation)
            {
                // The same class registered twice is not two implementations fighting over a name —
                // it is one assembly processed twice, which is a host wiring question and not a
                // reason to refuse to start. The registration already there stands, and the
                // disposable handed back is inert so disposing this one cannot remove it.
                _logger.LogDebug(
                    "Logic '{LogicType}' ({Family}) was already registered by {Implementation}; "
                        + "the existing registration stands.",
                    logicType,
                    key.Item2,
                    implementation.FullName
                );

                return new ActionDisposable(() => { });
            }

            _logger.LogError(
                "Logic '{LogicType}' ({Family}) is already registered by {Existing}; {Incoming} was "
                    + "refused. Two implementations of one logic name is a collision, not an "
                    + "override.",
                logicType,
                key.Item2,
                existing.Implementation?.FullName ?? "an unnamed registration",
                implementation.FullName
            );

            throw new VortexException(VortexErrorCodeEnum.InvalidLogic);
        }

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

            // Counted as well as logged. "The warning is the to-do list" was true and unusable: a
            // hotel cannot answer "how much of my catalogue is falling back" by reading logs.
            _metrics.FurnitureLogicFallback(logicType, family.ToString());

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
