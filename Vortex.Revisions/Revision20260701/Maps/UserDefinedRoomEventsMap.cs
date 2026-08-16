using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents;
using Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredmenu;
using Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredtrading;
using Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents;
using Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;
using Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class UserDefinedRoomEventsMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(MessageEvent.ApplySnapshotMessageEvent, new ApplySnapshotMessageParser());
        builder.MapParser(MessageEvent.OpenMessageEvent, new OpenMessageParser());
        builder.MapParser(MessageEvent.UpdateActionMessageEvent, new UpdateActionMessageParser());
        builder.MapParser(MessageEvent.UpdateAddonMessageEvent, new UpdateAddonMessageParser());
        builder.MapParser(
            MessageEvent.UpdateConditionMessageEvent,
            new UpdateConditionMessageParser()
        );
        builder.MapParser(
            MessageEvent.UpdateSelectorMessageEvent,
            new UpdateSelectorMessageParser()
        );
        builder.MapParser(MessageEvent.UpdateTriggerMessageEvent, new UpdateTriggerMessageParser());
        builder.MapParser(
            MessageEvent.UpdateVariableMessageEvent,
            new UpdateVariableMessageParser()
        );

        // Userdefinedroomevents Wiredmenu
        builder.MapParser(
            MessageEvent.WiredClearErrorLogsMessageEvent,
            new WiredClearErrorLogsMessageParser()
        );
        builder.MapParser(
            MessageEvent.WiredGetAllVariableHoldersMessageEvent,
            new WiredGetAllVariableHoldersMessageParser()
        );
        builder.MapParser(
            MessageEvent.WiredGetAllVariablesDiffsMessageEvent,
            new WiredGetAllVariablesDiffsMessageParser()
        );
        builder.MapParser(
            MessageEvent.WiredGetAllVariablesHashMessageEvent,
            new WiredGetAllVariablesHashMessageParser()
        );
        builder.MapParser(
            MessageEvent.WiredGetErrorLogsMessageEvent,
            new WiredGetErrorLogsMessageParser()
        );
        builder.MapParser(MessageEvent.WiredGetRoomLogsEvent, new WiredGetRoomLogsMessageParser());
        builder.MapParser(
            MessageEvent.WiredGetRoomSettingsMessageEvent,
            new WiredGetRoomSettingsMessageParser()
        );
        builder.MapParser(
            MessageEvent.WiredGetRoomStatsMessageEvent,
            new WiredGetRoomStatsMessageParser()
        );
        builder.MapParser(
            MessageEvent.WiredGetUserPermanentVariablesEvent,
            new WiredGetUserPermanentVariablesMessageParser()
        );
        builder.MapParser(
            MessageEvent.WiredGetVariableOwnersPageEvent,
            new WiredGetVariableOwnersPageMessageParser()
        );
        builder.MapParser(
            MessageEvent.WiredGetVariablesForObjectMessageEvent,
            new WiredGetVariablesForObjectMessageParser()
        );
        builder.MapParser(
            MessageEvent.WiredSetObjectVariableValueMessageEvent,
            new WiredSetObjectVariableValueMessageParser()
        );
        builder.MapParser(
            MessageEvent.WiredSetPreferencesMessageEvent,
            new WiredSetPreferencesMessageParser()
        );
        builder.MapParser(
            MessageEvent.WiredSetRoomSettingsMessageEvent,
            new WiredSetRoomSettingsMessageParser()
        );

        // Userdefinedroomevents Wiredtrading
        builder.MapParser(MessageEvent.OpenWiredChestEvent, new OpenWiredChestMessageParser());
        builder.MapParser(MessageEvent.CloseWiredChestEvent, new CloseWiredChestMessageParser());

        builder.MapSerializer(
            typeof(OpenEventMessageComposer),
            new OpenEventMessageComposerSerializer(MessageComposer.OpenComposer)
        );
        builder.MapSerializer(
            typeof(WiredFurniActionEventMessageComposer),
            new WiredFurniActionEventMessageComposerSerializer(
                MessageComposer.WiredFurniActionComposer
            )
        );
        builder.MapSerializer(
            typeof(WiredFurniAddonEventMessageComposer),
            new WiredFurniAddonEventMessageComposerSerializer(
                MessageComposer.WiredFurniAddonComposer
            )
        );
        builder.MapSerializer(
            typeof(WiredFurniConditionEventMessageComposer),
            new WiredFurniConditionEventMessageComposerSerializer(
                MessageComposer.WiredFurniConditionComposer
            )
        );
        builder.MapSerializer(
            typeof(WiredFurniSelectorEventMessageComposer),
            new WiredFurniSelectorEventMessageComposerSerializer(
                MessageComposer.WiredFurniSelectorComposer
            )
        );
        builder.MapSerializer(
            typeof(WiredFurniTriggerEventMessageComposer),
            new WiredFurniTriggerEventMessageComposerSerializer(
                MessageComposer.WiredFurniTriggerComposer
            )
        );
        builder.MapSerializer(
            typeof(WiredFurniVariableEventMessageComposer),
            new WiredFurniVariableEventMessageComposerSerializer(
                MessageComposer.WiredFurniVariableComposer
            )
        );
        builder.MapSerializer(
            typeof(WiredRewardResultMessageComposer),
            new WiredRewardResultMessageComposerSerializer(
                MessageComposer.WiredRewardResultMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(WiredSaveSuccessEventMessageComposer),
            new WiredSaveSuccessEventMessageComposerSerializer(
                MessageComposer.WiredSaveSuccessComposer
            )
        );
        builder.MapSerializer(
            typeof(WiredValidationErrorEventMessageComposer),
            new WiredValidationErrorEventMessageComposerSerializer(
                MessageComposer.WiredValidationErrorComposer
            )
        );
        builder.MapSerializer(
            typeof(WiredAllVariableHoldersEventMessageComposer),
            new WiredAllVariableHoldersEventMessageComposerSerializer(
                MessageComposer.WiredAllVariableHoldersComposer
            )
        );
        builder.MapSerializer(
            typeof(WiredAllVariablesDiffsEventMessageComposer),
            new WiredAllVariablesDiffsEventMessageComposerSerializer(
                MessageComposer.WiredAllVariablesDiffsComposer
            )
        );
        builder.MapSerializer(
            typeof(WiredAllVariablesHashEventMessageComposer),
            new WiredAllVariablesHashEventMessageComposerSerializer(
                MessageComposer.WiredAllVariablesHashComposer
            )
        );
        builder.MapSerializer(
            typeof(WiredErrorLogsEventMessageComposer),
            new WiredErrorLogsEventMessageComposerSerializer(MessageComposer.WiredErrorLogsComposer)
        );
        builder.MapSerializer(
            typeof(WiredMenuErrorEventMessageComposer),
            new WiredMenuErrorEventMessageComposerSerializer(MessageComposer.WiredMenuErrorComposer)
        );
        builder.MapSerializer(
            typeof(WiredPermissionsEventMessageComposer),
            new WiredPermissionsEventMessageComposerSerializer(
                MessageComposer.WiredPermissionsComposer
            )
        );
        builder.MapSerializer(
            typeof(WiredRoomSettingsEventMessageComposer),
            new WiredRoomSettingsEventMessageComposerSerializer(
                MessageComposer.WiredRoomSettingsComposer
            )
        );
        builder.MapSerializer(
            typeof(WiredRoomLogsComposer),
            new WiredRoomLogsComposerSerializer(MessageComposer.WiredRoomLogsComposer)
        );
        builder.MapSerializer(
            typeof(WiredRoomStatsEventMessageComposer),
            new WiredRoomStatsEventMessageComposerSerializer(MessageComposer.WiredRoomStatsComposer)
        );
        builder.MapSerializer(
            typeof(WiredSetUserPermanentVariableResultComposer),
            new WiredSetUserPermanentVariableResultComposerSerializer(
                MessageComposer.WiredSetUserPermanentVariableResultComposer
            )
        );
        builder.MapSerializer(
            typeof(WiredUserPermanentVariablesComposer),
            new WiredUserPermanentVariablesComposerSerializer(
                MessageComposer.WiredUserPermanentVariablesComposer
            )
        );
        builder.MapSerializer(
            typeof(WiredUserVariablesListComposer),
            new WiredUserVariablesListComposerSerializer(
                MessageComposer.WiredUserVariablesListComposer
            )
        );
        builder.MapSerializer(
            typeof(WiredVariablesForObjectEventMessageComposer),
            new WiredVariablesForObjectEventMessageComposerSerializer(
                MessageComposer.WiredVariablesForObjectComposer
            )
        );

        builder.MapSerializer(
            typeof(WiredChestOpenMessageComposer),
            new WiredChestOpenMessageComposerSerializer(MessageComposer.WiredChestOpenComposer)
        );
        builder.MapSerializer(
            typeof(WiredChestCoinsMessageComposer),
            new WiredChestCoinsMessageComposerSerializer(MessageComposer.WiredChestCoinsComposer)
        );
    }
}
