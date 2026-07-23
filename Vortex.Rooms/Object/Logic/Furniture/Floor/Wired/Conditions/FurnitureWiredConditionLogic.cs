using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

public abstract class FurnitureWiredConditionLogic(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredLogic(grainFactory, stuffDataFactory, ctx), IWiredCondition
{
    public override WiredType WiredType => WiredType.Condition;

    private int _quantifierCode = 0;
    private bool _isInvert = false;
    private byte _quantifierType = 0;

    // Wire layout must match the client (ConditionDefinition in WIN63 AS3 _SafeCls_3155):
    //   readDefinitionSpecifics() reads only quantifierCode (int), before advancedMode + InputSourcesConf.
    //   readTypeSpecifics() reads quantifierType (byte) then isInvert (bool), after InputSourcesConf.
    // isInvert therefore belongs to the TYPE specifics, not the definition specifics. Putting it in the
    // definition specifics writes an extra bool ahead of InputSourcesConf and desyncs the client parse
    // (RangeError: End of buffer inside InputSourcesConf.readAllowedSources).
    public override List<Type> GetDefinitionSpecificTypes() =>
        [.. base.GetDefinitionSpecificTypes(), typeof(int)];

    public override List<Type> GetTypeSpecificTypes() =>
        [.. base.GetTypeSpecificTypes(), typeof(byte), typeof(bool)];

    public int GetQuantifierCode() => _quantifierCode;

    public bool GetIsInvert() => _isInvert;

    public byte GetQuantifierType() => _quantifierType;

    public virtual bool IsNegative() => false;

    public virtual bool Evaluate(IWiredProcessingContext ctx) => false;

    protected override async Task FillInternalDataAsync(CancellationToken ct)
    {
        await base.FillInternalDataAsync(ct);

        try
        {
            _quantifierCode = _wiredData.GetDefinitionParam<int>(0);
            _quantifierType = _wiredData.GetTypeParam<byte>(0);
            _isInvert = _wiredData.GetTypeParam<bool>(1);
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Malformed condition params for wired item {ItemId}; keeping current defaults.",
                _ctx.ObjectId
            );
        }
    }
}
