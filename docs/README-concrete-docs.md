# Turbo Cloud — Concrete Docs

These documents fill the gap between the project's **prescriptive** docs (which say
*where* code goes and *what rules* to follow — `CONTEXT.md`, `AGENTS.md`) and the code
itself. They are **demonstrative**: they show a real feature moving through every layer,
end to end, using the actual signatures in the repository.

Drop these into the existing `docs/` tree. Suggested placement:

```
docs/
├── orleans.md                      (existing)
├── glossary.md                     ← NEW
├── patterns/
│   ├── HandlerPattern.cs           (existing)
│   ├── ServicePattern.cs           (existing)
│   ├── UnitTestPattern.cs          (existing)
│   └── vertical-slice.md           ← NEW
└── walkthroughs/                   ← NEW folder
    ├── request-lifecycle.md        ← NEW
    └── add-a-feature.md            ← NEW
```

## What each doc is for

| Doc | Question it answers | Read it when |
|---|---|---|
| **`walkthroughs/request-lifecycle.md`** | "How does one packet travel from socket to client and back?" | You're new, or you need to understand the grain + stream + presence flow concretely. Start here. |
| **`walkthroughs/add-a-feature.md`** | "How do I add a new client-driven feature across every layer?" | You're about to implement anything. This is the hello-world. |
| **`patterns/vertical-slice.md`** | "How do handler, grain logic, and test collaborate on *one* feature?" | The three single-layer pattern samples left you unsure how they connect. |
| **`glossary.md`** | "What does *controller level* / *presence grain* / *composer* / *wired condition* mean, and where's the code?" | Any time a term is unfamiliar. Every entry links to the file that implements it. |

## Reading order for a new contributor

1. `README.md` (repo root) — get it building and running.
2. `docs/orleans.md` — the mental model (grains, streams, snapshots).
3. **`docs/walkthroughs/request-lifecycle.md`** — that model in motion on one real packet.
4. **`docs/glossary.md`** — skim; keep it open as a reference.
5. **`docs/walkthroughs/add-a-feature.md`** — now build something.
6. **`docs/patterns/vertical-slice.md`** — when you write the handler/grain/test together.
7. `CONTEXT.md` + `AGENTS.md` — the rules, which now read as "why," not just "what."

## A note on fidelity

Every code excerpt in `request-lifecycle.md` is taken from the shipping code (the chat
flow: `ChatMessageHandler` → `RoomGrain.Avatar.cs` → `RoomChatSystem` →
`SendComposerToRoomAsync` → `PlayerPresenceGrain.Room.cs`). The `add-a-feature.md` and
`vertical-slice.md` "wave" feature is illustrative — it does not exist in the repo yet —
but it is written strictly against real shapes (`IMessageHandler<T>`, `MessageContext`,
the `IGrainFactory.GetRoomGrain` → interface → partial → module path, `IComposer`,
`SendComposerToRoomAsync`). If those shapes change, update the docs alongside them.

## The one gap these docs deliberately highlight

`docs/patterns/UnitTestPattern.cs` exists, `AGENTS.md` requires test validation, and
`vertical-slice.md` shows the failure-path-first test shape — yet **the repository
currently ships no tests.** These docs describe the testing discipline the project has
tooled for; closing the gap between that and reality (a test project exercising the
grains while the surface is still small) is the highest-leverage next step.
