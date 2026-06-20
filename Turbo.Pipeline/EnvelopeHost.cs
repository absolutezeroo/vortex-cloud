using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Pipeline.Delegates;
using Turbo.Pipeline.Registry;
using Turbo.Runtime;

namespace Turbo.Pipeline;

public class EnvelopeHost<TEnvelope, TMeta, TContext>(
    IServiceProvider host,
    EnvelopeHostOptions<TEnvelope, TMeta, TContext> options
)
{
    private readonly IServiceProvider _host = host;
    private readonly EnvelopeHostOptions<TEnvelope, TMeta, TContext> _opt = options;
    private readonly ConcurrentDictionary<Type, Bucket<TContext>> _byEvent = new();

    public IDisposable RegisterHandler(
        Type envType,
        IServiceProvider sp,
        Func<IServiceProvider, object> activator,
        HandlerInvoker<TContext> invoker
    )
    {
        ArgumentNullException.ThrowIfNull(envType);

        Bucket<TContext> b = _byEvent.GetOrAdd(envType, _ => new Bucket<TContext>());

        lock (b.Gate)
        {
            b.Handlers = b.Handlers.Add(new HandlerReg<TContext>(sp, activator, invoker));
            b.Version++;
            InvalidateCache(b);
        }

        return new ActionDisposable(() =>
        {
            lock (b.Gate)
            {
                b.Handlers = b.Handlers.RemoveAll(h =>
                    h.Activator == activator && h.Invoker == invoker
                );
                b.Version++;
                InvalidateCache(b);
            }
        });
    }

    public IDisposable RegisterBehavior(
        Type envType,
        IServiceProvider sp,
        Func<IServiceProvider, object> activator,
        BehaviorInvoker<TContext> invoker,
        int order
    )
    {
        ArgumentNullException.ThrowIfNull(envType);

        Bucket<TContext> b = _byEvent.GetOrAdd(envType, _ => new Bucket<TContext>());

        lock (b.Gate)
        {
            b.Behaviors = b.Behaviors.Add(new BehaviorReg<TContext>(sp, order, activator, invoker));
            b.Version++;
            InvalidateCache(b);
        }

        return new ActionDisposable(() =>
        {
            lock (b.Gate)
            {
                b.Behaviors = b.Behaviors.RemoveAll(x =>
                    x.Activator == activator && x.Invoker == invoker && x.Order == order
                );
                b.Version++;
                InvalidateCache(b);
            }
        });
    }

    public async Task PublishAsync(TEnvelope env, TMeta? meta, CancellationToken ct)
    {
        if (env is null)
        {
            return;
        }

        Type t = env.GetType();

        if (!_byEvent.TryGetValue(t, out Bucket<TContext>? bucket) && !_opt.EnableInheritanceDispatch)
        {
            return;
        }

        Func<object, TContext, CancellationToken, ValueTask>? pipeline = GetOrBuildPipeline(t, bucket);

        if (pipeline is null)
        {
            return;
        }

        TContext ctx = await _opt.CreateContextAsync(env, meta).ConfigureAwait(false);

        await pipeline(env, ctx, ct).ConfigureAwait(false);
    }

    private Func<object, TContext, CancellationToken, ValueTask>? GetOrBuildPipeline(
        Type envType,
        Bucket<TContext>? primaryBucket
    )
    {
        if (!_opt.EnableInheritanceDispatch)
        {
            if (primaryBucket is null)
            {
                return null;
            }

            Func<object, TContext, CancellationToken, ValueTask>? cached = Volatile.Read(ref primaryBucket.CachedPipeline);

            if (
                cached is not null
                && primaryBucket.CachedVersion == primaryBucket.Version
                && primaryBucket.CachedForEnvType == envType
            )
            {
                return cached;
            }

            lock (primaryBucket.Gate)
            {
                if (
                    primaryBucket.CachedPipeline is not null
                    && primaryBucket.CachedVersion == primaryBucket.Version
                    && primaryBucket.CachedForEnvType == envType
                )
                {
                    return primaryBucket.CachedPipeline;
                }

                Func<object, TContext, CancellationToken, ValueTask> pipeline = BuildPipeline(primaryBucket.Handlers, primaryBucket.Behaviors);

                primaryBucket.CachedPipeline = pipeline;
                primaryBucket.CachedVersion = primaryBucket.Version;
                primaryBucket.CachedForEnvType = envType;

                return pipeline;
            }
        }

        (ImmutableArray<HandlerReg<TContext>> handlers, ImmutableArray<BehaviorReg<TContext>> behaviors, int globalVersion) = ResolveForType(envType);

        primaryBucket ??= _byEvent.GetOrAdd(envType, _ => new Bucket<TContext>());

        Func<object, TContext, CancellationToken, ValueTask>? cached2 = Volatile.Read(ref primaryBucket.CachedPipeline);

        if (
            cached2 is not null
            && primaryBucket.CachedVersion == globalVersion
            && primaryBucket.CachedForEnvType == envType
        )
        {
            return cached2;
        }

        lock (primaryBucket.Gate)
        {
            if (
                primaryBucket.CachedPipeline is not null
                && primaryBucket.CachedVersion == globalVersion
                && primaryBucket.CachedForEnvType == envType
            )
            {
                return primaryBucket.CachedPipeline;
            }

            Func<object, TContext, CancellationToken, ValueTask> pipeline = BuildPipeline(handlers, behaviors);

            primaryBucket.CachedPipeline = pipeline;
            primaryBucket.CachedVersion = globalVersion;
            primaryBucket.CachedForEnvType = envType;

            return pipeline;
        }

        (
            ImmutableArray<HandlerReg<TContext>>,
            ImmutableArray<BehaviorReg<TContext>>,
            int
        ) ResolveForType(Type t)
        {
            IEnumerable<Type> types = EnumerateTypeGraph(t);
            ImmutableArray<HandlerReg<TContext>>.Builder handlerBuilder = ImmutableArray.CreateBuilder<HandlerReg<TContext>>();
            ImmutableArray<BehaviorReg<TContext>>.Builder behaviorBuilder = ImmutableArray.CreateBuilder<BehaviorReg<TContext>>();
            int versionSum = 0;

            foreach (Type tp in types)
            {
                if (_byEvent.TryGetValue(tp, out Bucket<TContext>? b))
                {
                    versionSum = unchecked(versionSum + b.Version);
                    handlerBuilder.AddRange(b.Handlers);
                    behaviorBuilder.AddRange(b.Behaviors);
                }
            }

            ImmutableArray<BehaviorReg<TContext>> behaviors = behaviorBuilder
                .ToImmutable()
                .Sort(
                    static (a, b) =>
                    {
                        int cmp = a.Order.CompareTo(b.Order);
                        return cmp != 0 ? cmp : 0;
                    }
                );

            return (handlerBuilder.ToImmutable(), behaviors, versionSum);
        }

        static IEnumerable<Type> EnumerateTypeGraph(Type t)
        {
            yield return t;

            for (Type? cur = t.BaseType; cur is not null; cur = cur.BaseType)
                yield return cur;

            foreach (Type iface in t.GetInterfaces())
                yield return iface;
        }
    }

    private Func<object, TContext, CancellationToken, ValueTask> BuildPipeline(
        ImmutableArray<HandlerReg<TContext>> handlers,
        ImmutableArray<BehaviorReg<TContext>> behaviors
    )
    {
        return async (env, ctx, ct) =>
        {
            CompositeServiceProviderBag bag = new CompositeServiceProviderBag(_host);

            Func<object, TContext, CancellationToken, ValueTask> terminal = async (env, ctx, ct) =>
                await InvokeHandlersAsync(handlers, env, ctx, ct).ConfigureAwait(false);

            for (int i = behaviors.Length - 1; i >= 0; i--)
            {
                BehaviorReg<TContext> beh = behaviors[i];
                Func<object, TContext, CancellationToken, ValueTask> next = terminal;

                terminal = async (env, ctx, ct) =>
                {
                    IServiceProvider sp = bag.Get(beh.ServiceProvider);

                    object? inst = null;

                    try
                    {
                        inst = beh.Activator(sp);
                    }
                    catch (Exception ex)
                    {
                        _opt.OnBehaviorActivationError?.Invoke(ex, env);

                        await next(env, ctx, ct).ConfigureAwait(false);

                        return;
                    }

                    try
                    {
                        await beh.Invoker(
                                inst,
                                env,
                                ctx,
                                async () => await next(env, ctx, ct).ConfigureAwait(false),
                                ct
                            )
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _opt.OnBehaviorInvokeError?.Invoke(ex, env);
                    }
                    finally
                    {
                        if (inst is IAsyncDisposable iad)
                        {
                            await iad.DisposeAsync().ConfigureAwait(false);
                        }
                        else if (inst is IDisposable d)
                        {
                            d.Dispose();
                        }
                    }
                };
            }

            await terminal(env, ctx, ct).ConfigureAwait(false);
        };
    }

    private async ValueTask InvokeHandlersAsync(
        ImmutableArray<HandlerReg<TContext>> regs,
        object env,
        TContext ctx,
        CancellationToken ct
    )
    {
        if (regs.IsDefaultOrEmpty)
        {
            return;
        }

        if (_opt.HandlerMode == HandlerExecutionMode.Sequential)
        {
            for (int i = 0; i < regs.Length; i++)
                await InvokeOneAsync(regs[i], env, ctx, ct).ConfigureAwait(false);

            return;
        }

        if (regs.Length == 1)
        {
            await InvokeOneAsync(regs[0], env, ctx, ct).ConfigureAwait(false);

            return;
        }

        if (_opt.MaxHandlerDegreeOfParallelism is int dop && dop > 0 && dop < regs.Length)
        {
            List<Func<CancellationToken, ValueTask>> work = new List<Func<CancellationToken, ValueTask>>(regs.Length);

            for (int i = 0; i < regs.Length; i++)
            {
                HandlerReg<TContext> r = regs[i];
                work.Add(token => InvokeOneAsync(r, env, ctx, token));
            }

            await BoundedHelper.RunAsync(work, dop, ct).ConfigureAwait(false);
        }
        else
        {
            Task[] tasks = new Task[regs.Length];

            for (int i = 0; i < regs.Length; i++)
                tasks[i] = InvokeOneAsync(regs[i], env, ctx, ct).AsTask();

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    private async ValueTask InvokeOneAsync(
        HandlerReg<TContext> h,
        object env,
        TContext ctx,
        CancellationToken ct
    )
    {
        IServiceProvider sp = h.ServiceProvider;

        if (sp != _host)
        {
            sp = new CompositeServiceProvider(sp, _host);
        }

        object? inst = null;

        try
        {
            inst = h.Activator(sp);
        }
        catch (Exception ex)
        {
            _opt.OnHandlerActivationError?.Invoke(ex, env);

            return;
        }

        try
        {
            await h.Invoker(inst, env, ctx, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _opt.OnHandlerInvokeError?.Invoke(ex, env);
        }
        finally
        {
            if (inst is IAsyncDisposable iad)
            {
                await iad.DisposeAsync().ConfigureAwait(false);
            }
            else if (inst is IDisposable d)
            {
                d.Dispose();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InvalidateCache(Bucket<TContext> b)
    {
        b.CachedPipeline = null;
        b.CachedForEnvType = null;
        b.CachedVersion = 0;
    }
}
