using System.Collections.Generic;
using Turbo.Primitives.Catalog.Enums;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260701.Serializers.Catalog.Data;

namespace Turbo.Revisions.Revision20260701.Serializers.Catalog;

internal class CatalogIndexMessageComposerSerializer(int header)
    : AbstractSerializer<CatalogIndexMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CatalogIndexMessageComposer message)
    {
        if (
            !message.Catalog.PagesById.TryGetValue(
                message.Catalog.RootPageId,
                out CatalogPageSnapshot? rootPage
            )
        )
        {
            return;
        }

        SerializePage(packet, message.Catalog, rootPage);

        packet
            .WriteBoolean(message.NewAdditionsAvailable)
            .WriteString(message.Catalog.CatalogType.ToLegacyString());
    }

    private static void SerializePage(
        IServerPacket packet,
        CatalogSnapshot snapshot,
        CatalogPageSnapshot page
    )
    {
        CatalogPageSnapshotSerializer.Serialize(packet, page);

        packet.WriteInteger(page.OfferIds.Length);

        foreach (int offerId in page.OfferIds)
        {
            packet.WriteInteger(offerId);
        }

        List<CatalogPageSnapshot> children = [];

        foreach (int childId in page.ChildIds)
        {
            if (
                snapshot.PagesById.TryGetValue(childId, out CatalogPageSnapshot? childPage)
                && childPage.Visible
            )
            {
                children.Add(childPage);
            }
        }

        packet.WriteInteger(children.Count);

        foreach (CatalogPageSnapshot child in children)
        {
            SerializePage(packet, snapshot, child);
        }
    }
}
