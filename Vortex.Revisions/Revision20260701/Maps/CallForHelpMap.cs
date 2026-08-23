using Vortex.Primitives.Networking.Revisions;
using Vortex.Protocol.Messages.Outgoing.Callforhelp;
using Vortex.Revisions.Revision20260701.Serializers.CallForHelp;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class CallForHelpMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapSerializer(
            typeof(CfhSanctionMessageComposer),
            new CfhSanctionMessageComposerSerializer(MessageComposer.CfhSanctionMessageComposer)
        );
        builder.MapSerializer(
            typeof(CfhTopicsInitMessageComposer),
            new CfhTopicsInitMessageComposerSerializer(MessageComposer.CfhTopicsInitMessageComposer)
        );
        builder.MapSerializer(
            typeof(SanctionStatusEventMessageComposer),
            new SanctionStatusEventMessageComposerSerializer(MessageComposer.SanctionStatusComposer)
        );
    }
}
