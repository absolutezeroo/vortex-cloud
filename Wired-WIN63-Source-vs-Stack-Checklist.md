# Wireds Implementation Audit (WIN63-2026 vs Vortex Stack)

## Objective
- Produce a clean, AI-ready checklist for implementing missing WIN63 wired functionality in Vortex.
- Keep only explicit, stable file names (no obfuscated `class_xxx` references in the checklist body).

## Scope
- Focus: `userdefinedroomevents` (wired logic only).
- Excluded: renderer/room-visual modules (wall, plane, rasterizer, etc.).
- Reference protocol revision: `2026-01-12` (`Revision20260112`).

## Explicit file mapping policy
Use this exact file-naming convention in the checklist:
1. `WIN63 source file` → `Suggested semantic name`
2. `Vortex C# file to create/modify`
3. `Protocol slot` (`MessageEvent` / `MessageComposer` / Parser / Serializer / Handler)

## Obfuscated WIN63 class aliases (keep only for source-side traceability)
- `class_2397.as` => `WiredDefinitionBase` (base wired payload structure)
- `class_2531.as` => `WiredQuantifierDefinition` (`quantifierType`, `quantifierCode`, `isInvert`)
- `class_3034.as` => `WiredTriggerDefinition`
- `class_3042.as` => `WiredActionDefinition` (`delayInPulses`)
- `class_3381.as` => `WiredVariableDefinition`
- `class_3525.as` => `WiredAddonDefinition`
- `class_3153.as` => `WiredQuantifierTypeConstants`
- `class_3569.as` => `WiredValidationPair` (`key`, `value`)

## What already exists in current stack

### Messages and composers implemented
- `OpenMessage.cs` / `OpenEventMessageComposer.cs`
- `ApplySnapshotMessage.cs`
- `UpdateActionMessage.cs` / `UpdateActionMessageParser.cs`
- `UpdateAddonMessage.cs` / `UpdateAddonMessageParser.cs`
- `UpdateConditionMessage.cs` / `UpdateConditionMessageParser.cs`
- `UpdateSelectorMessage.cs` / `UpdateSelectorMessageParser.cs`
- `UpdateTriggerMessage.cs` / `UpdateTriggerMessageParser.cs`
- `UpdateVariableMessage.cs` / `UpdateVariableMessageParser.cs`
- `WiredFurniActionEventMessageComposer.cs` (+ serializer)
- `WiredFurniAddonEventMessageComposer.cs` (+ serializer)
- `WiredFurniConditionEventMessageComposer.cs` (+ serializer)
- `WiredFurniSelectorEventMessageComposer.cs` (+ serializer)
- `WiredFurniTriggerEventMessageComposer.cs` (+ serializer)
- `WiredFurniVariableEventMessageComposer.cs` (+ serializer)
- `WiredRewardResultMessageComposer.cs` (+ serializer)
- `WiredSaveSuccessEventMessageComposer.cs` (+ serializer)
- `WiredValidationErrorEventMessageComposer.cs` (+ serializer)
- Wired menu:
  - `WiredClearErrorLogsMessage.cs`
  - `WiredGetAllVariableHoldersMessage.cs`
  - `WiredGetAllVariablesDiffsMessage.cs`
  - `WiredGetAllVariablesHashMessage.cs`
  - `WiredGetErrorLogsMessage.cs`
  - `WiredGetRoomSettingsMessage.cs`
  - `WiredGetRoomStatsMessage.cs`
  - `WiredGetVariablesForObjectMessage.cs`
  - `WiredSetObjectVariableValueMessage.cs`
  - `WiredSetPreferencesMessage.cs`
  - `WiredSetRoomSettingsMessage.cs`
  - their parsers in `Parsers/Userdefinedroomevents/Wiredmenu`
  - all corresponding composers (`Wiredmenu/*EventMessageComposer.cs`) and serializers
  - all corresponding handlers in `PacketHandlers/Userdefinedroomevents/Wiredmenu`

## Missing implementation (high signal, must not be overlooked)

### P0 (critical)
#### 1) WiredRoomLogs flow
- Protocol IDs exist in `Revision20260112/Headers.cs`:
  - `MessageEvent.WiredGetRoomLogsEvent`
  - `MessageComposer.WiredRoomLogsComposer`
- Current stack status: missing end-to-end.
- Required files/entries:
  - Incoming contract:
    - `Turbo.Primitives/Messages/Incoming/Userdefinedroomevents/Wiredmenu/WiredGetRoomLogsMessage.cs`
  - Outgoing contract:
    - `Turbo.Primitives/Messages/Outgoing/Userdefinedroomevents/Wiredmenu/WiredRoomLogsEventMessageComposer.cs`
  - Parser:
    - `Turbo.Revisions/Revision20260112/Parsers/Userdefinedroomevents/Wiredmenu/WiredRoomLogsEventParser.cs`
  - Serializer:
    - `Turbo.Revisions/Revision20260112/Serializers/Userdefinedroomevents/Wiredmenu/WiredRoomLogsEventMessageComposerSerializer.cs`
  - Packet handler:
    - `Turbo.PacketHandlers/Userdefinedroomevents/Wiredmenu/WiredGetRoomLogsMessageHandler.cs`
  - Register in:
    - `Turbo.Revisions/Revision20260112/Revision20260112.cs` (incoming + outgoing dictionaries)
- WIN63 files to copy semantics from:
  - `WiredRoomLogsEvent.as`
  - `WiredRoomLogsEventParser.as`
  - `WiredLogPage.as`
  - `WiredLogEntry.as`
  - `WiredGetRoomLogsComposer.as`

#### 2) WiredClickUser request/response flow
- Protocol IDs exist in `Revision20260112/Headers.cs`:
  - `MessageEvent.WiredClickUserMessageEvent`
  - `MessageComposer.WiredClickUserResponseComposer`
- Current stack status: missing end-to-end.
- Required files/entries:
  - Incoming contract:
    - `Turbo.Primitives/Messages/Incoming/Userdefinedroomevents/Wiredmenu/WiredClickUserMessage.cs`
  - Outgoing contract:
    - `Turbo.Primitives/Messages/Outgoing/Userdefinedroomevents/Wiredmenu/WiredClickUserResponseEventMessageComposer.cs`
  - Parser:
    - `Turbo.Revisions/Revision20260112/Parsers/Userdefinedroomevents/Wiredmenu/WiredClickUserMessageParser.cs`
  - Serializer:
    - `Turbo.Revisions/Revision20260112/Serializers/Userdefinedroomevents/Wiredmenu/WiredClickUserResponseEventMessageComposerSerializer.cs`
  - Packet handler:
    - `Turbo.PacketHandlers/Userdefinedroomevents/Wiredmenu/WiredClickUserMessageHandler.cs`
  - Register in:
    - `Turbo.Revisions/Revision20260112/Revision20260112.cs` (incoming + outgoing dictionaries)
- WIN63 files to copy semantics from:
  - `WiredClickUserMessageComposer.as`
  - `WiredClickUserResponseEvent.as`
  - `WiredClickUserResponseEventParser.as`
  - `WiredClickUserEventParser.as` (if split in source)

### P1 (important)
#### 3) Wired protocol IDs declared but not implemented
- `MessageEvent.WiredGetUserPermanentVariablesEvent`
- `MessageEvent.WiredGetVariableOwnersPageEvent`
- `MessageEvent.WiredSetUserPermanentVariableEvent`
- `MessageComposer.WiredSetUserPermanentVariableResult`
- `MessageEvent.WiredUpdateRoomEvent`
- Add/remove according to source evidence:
  - add message contracts
  - add parser/serializer
  - add revision registrations
  - add handlers where required
- If source proves they are unused/legacy in WIN63, mark explicit "intentionally not implemented".

## Mandatory execution order for AI
1. Implement `WiredRoomLogs`.
2. Implement `WiredClickUser` + `WiredClickUserResponse`.
3. Resolve declared-but-missing P1 IDs.
4. Run one final cross-check:
   - `Headers.cs` contains ID
   - `Parsers` has incoming mapping
   - `Serializers` has outgoing mapping
   - `PacketHandlers` has request handler (if client-originated)

## Quick file naming rules (to prevent messy docs)
- Use names like `Wiredmenu\WiredGetRoomLogs...` and `Wiredmenu\WiredClickUser...`.
- Avoid writing `class_3042` or `as`-only names in status sheets.
- Keep `Wired` tasks in this file as:
  - `WireTask-01-WiredRoomLogs.md`
  - `WireTask-02-WiredClickUser.md`
  - `WireTask-03-P1-LegacyWiredEvents.md` (if needed).
