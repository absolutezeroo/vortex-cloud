using Vortex.Primitives.Networking.Revisions;
using Vortex.Protocol.Messages.Outgoing.Perk;
using Vortex.Revisions.Revision20260701.Serializers.Perk;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class PerkMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapSerializer(
            typeof(PerkAllowancesMessageComposer),
            new PerkAllowancesMessageComposerSerializer(
                MessageComposer.PerkAllowancesMessageComposer
            )
        );
    }
}
