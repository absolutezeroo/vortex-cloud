using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Providers;
using Vortex.Runtime;
using Vortex.Runtime.AssemblyProcessing;

namespace Vortex.Rooms.Object.Logic;

internal class RoomObjectLogicFeatureProcessor(IRoomObjectLogicProvider roomObjectLogicFactory)
    : IAssemblyFeatureProcessor
{
    private readonly IRoomObjectLogicProvider _roomObjectLogicFactory = roomObjectLogicFactory;

    public Task<IDisposable> ProcessAsync(
        Assembly asm,
        IServiceProvider sp,
        CancellationToken ct = default
    )
    {
        CompositeDisposable batch = new CompositeDisposable();

        foreach (Type? concrete in AssemblyExplorer.FindAssignees(asm, typeof(IRoomObjectLogic)))
        {
            if (concrete is null)
            {
                continue;
            }

            foreach (
                RoomObjectLogicAttribute attribute in concrete.GetCustomAttributes<RoomObjectLogicAttribute>(
                    false
                )
            )
            {
                Type logicType = concrete;

                batch.Add(
                    _roomObjectLogicFactory.RegisterLogic(
                        attribute.Key,
                        logicType,
                        sp,
                        (sp, ctx) =>
                            (IRoomObjectLogic)ActivatorUtilities.CreateInstance(sp, logicType, ctx)
                    )
                );
            }
        }

        return Task.FromResult<IDisposable>(batch);
    }
}
