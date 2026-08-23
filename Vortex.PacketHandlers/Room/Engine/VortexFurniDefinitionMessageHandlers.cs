using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Furniture;
using Vortex.Primitives.Furniture.Admin;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Protocol.Messages.Incoming.Room.Engine;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.PacketHandlers.Room.Engine;

/// <summary>
/// Shared shaping for the definition editor's two handlers. Vortex-specific: no AS3 or Habbo
/// equivalent.
/// </summary>
internal static class VortexFurniDefinitionResponse
{
    public static VortexFurniDefinitionMessageComposer Build(
        FurnitureDefinitionSnapshot definition,
        string error = ""
    ) =>
        new()
        {
            DefinitionId = definition.Id,
            SpriteId = definition.SpriteId,
            Name = definition.Name,
            ProductType = definition.ProductType,
            FurniCategory = definition.FurniCategory,
            Logic = definition.LogicName,
            TotalStates = definition.TotalStates,
            Width = definition.Width,
            Length = definition.Length,
            StackHeightHundredths = definition.StackHeight.ToInt(),
            CanStack = definition.CanStack,
            CanWalk = definition.CanWalk,
            CanSit = definition.CanSit,
            CanLay = definition.CanLay,
            CanRecycle = definition.CanRecycle,
            CanTrade = definition.CanTrade,
            CanGroup = definition.CanGroup,
            CanSell = definition.CanSell,
            UsagePolicy = definition.UsagePolicy,
            ExtraData = string.Empty,
            StuffDataType = definition.StuffDataType,
            Error = error,
        };
}

/// <summary>
/// Serves one furniture definition to the in-client definition editor. Vortex-specific.
///
/// Silence is the answer to an unauthorized request: the editor never opens without this reply, so
/// a caller without <c>furniture.definition.edit</c> gets no window at all.
/// </summary>
public class VortexGetFurniDefinitionMessageHandler(
    IPermissionService permissionService,
    IFurnitureDefinitionProvider definitionProvider
) : IMessageHandler<VortexGetFurniDefinitionMessage>
{
    public async ValueTask HandleAsync(
        VortexGetFurniDefinitionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        PermissionSet permissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (!permissions.Has(Capabilities.Furniture.DefinitionEdit))
        {
            return;
        }

        FurnitureDefinitionSnapshot? definition = definitionProvider.TryGetDefinition(
            message.DefinitionId
        );

        if (definition is null)
        {
            return;
        }

        await ctx.SendComposerAsync(VortexFurniDefinitionResponse.Build(definition), ct)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Rewrites one furniture definition from the in-client editor. Vortex-specific.
///
/// The write itself is <see cref="IFurnitureAdminService.UpdateAsync"/> — the same service the
/// dashboard's furniture admin uses — so the duplicate sprite/type/category guard and the
/// provider reload after every write are inherited rather than reimplemented. That reload is what
/// makes the edit live: every handler and serializer reads the definition snapshot the provider
/// hands out, so the new interaction, footprint and flags take effect for every placed copy without
/// a restart.
///
/// The client's *rendering* is a different matter and is not affected: geometry and animation come
/// from the furnidata/.nitro assets, not from this table.
/// </summary>
public class VortexApplyFurniDefinitionMessageHandler(
    IPermissionService permissionService,
    IFurnitureDefinitionProvider definitionProvider,
    IFurnitureAdminService adminService
) : IMessageHandler<VortexApplyFurniDefinitionMessage>
{
    public async ValueTask HandleAsync(
        VortexApplyFurniDefinitionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        PermissionSet permissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (!permissions.Has(Capabilities.Furniture.DefinitionEdit))
        {
            return;
        }

        FurnitureDefinitionUpsertSpec spec = new(
            SpriteId: message.SpriteId,
            Name: message.Name,
            ProductType: message.ProductType,
            FurniCategory: message.FurniCategory,
            Logic: message.Logic,
            TotalStates: message.TotalStates,
            Width: message.Width,
            Length: message.Length,
            StackHeight: Altitude.FromInt(message.StackHeightHundredths).Value,
            CanStack: message.CanStack,
            CanWalk: message.CanWalk,
            CanSit: message.CanSit,
            CanLay: message.CanLay,
            CanRecycle: message.CanRecycle,
            CanTrade: message.CanTrade,
            CanGroup: message.CanGroup,
            CanSell: message.CanSell,
            UsagePolicy: message.UsagePolicy,
            ExtraData: string.IsNullOrEmpty(message.ExtraData) ? null : message.ExtraData,
            StuffDataType: message.StuffDataType
        );

        FurnitureAdminResult result = await adminService
            .UpdateAsync(message.DefinitionId, spec, ct)
            .ConfigureAwait(false);

        // Read back through the provider rather than echoing the spec: after a successful write the
        // provider has reloaded, so this is the stored row, and after a failure it is the untouched
        // one — either way the editor redraws from the server's truth.
        FurnitureDefinitionSnapshot? definition = definitionProvider.TryGetDefinition(
            message.DefinitionId
        );

        if (definition is null)
        {
            return;
        }

        await ctx.SendComposerAsync(
                VortexFurniDefinitionResponse.Build(definition, result.ErrorCode ?? string.Empty),
                ct
            )
            .ConfigureAwait(false);
    }
}
