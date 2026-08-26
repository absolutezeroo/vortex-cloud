# Vertical slice workflow

## 1. SYNC

- read completeness state;
- verify codebase documentation freshness;
- regenerate completeness;
- select one obligation.

## 2. EVIDENCE

Read:

- relevant codebase docs;
- target client path;
- `habbo-spec analyze`;
- conflicts and unknowns;
- current source.

## 3. CONTRACT

Write before editing:

```text
OBLIGATION
CLIENT ENTRYPOINT
CURRENT STATUS
CURRENT VORTEX FLOW
ACTUAL DOMAIN OWNER
EXPECTED CLIENT-VISIBLE RESULT
PROTOCOL EVIDENCE
OPEN UNKNOWNS / CONFLICTS
FILES I EXPECT TO TOUCH
FILES I WILL NOT TOUCH
PERSISTENCE IMPACT
VALUE / SECURITY IMPACT
TESTS REQUIRED
ROLLBACK CONDITION
DONE WHEN
```

## 4. IMPLEMENT

One coherent vertical slice. No unrelated cleanup and no architecture drift.

## 5. VERIFY

```bash
dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudFastCheck
dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudQualityGate
dotnet run --project Vortex.Specs.Cli -- validate
dotnet run --project Vortex.Specs.Cli -- completeness --domain <domain>
```

Run the wire conflict/header hooks when protocol files changed.

## 6. DOCUMENT

If runtime behaviour/ownership/flow changed, update `docs/codebase/` through `/document-vortex update`
or the relevant targeted mode.

If new protocol evidence was added, regenerate Habbo specs through the existing `habbo-spec` workflow.

## 7. RE-MEASURE

Run completeness again.

The slice closes only if the status moves for a demonstrated reason or if the work produces an
evidence-backed UNKNOWN/N/A decision that is itself the intended outcome.
