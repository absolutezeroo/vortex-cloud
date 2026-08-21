# Working from the Habbo Specs

The specs under `docs/habbo-specs/` are the reference for anything that touches Habbo protocol
behaviour. This is how to use them, and how to put what you learn back.

## Before changing protocol behaviour

```bash
dotnet run --project Vortex.Specs.Cli -- analyze <feature-id or PacketName>
```

That prints, for one feature or packet:

- the packet's field layout and how well each field's name and type are attested;
- which layer of this emulator currently handles it, and what it does on the way through;
- the guards it applies and the state it changes;
- what goes back out and to whom;
- every conflict and every open question that touches it;
- the scenarios generated from its own guards.

Read the conflicts and unknowns before writing code. They are the parts where the obvious change is
a guess.

## The rules

1. Find the relevant spec. `analyze` takes a feature id (`room.move_floor_item_in_room`) or a packet
   name (`MoveObject`); `conflicts` and `unknowns` list what is open across the whole tree.
2. Respect `client_confirmed` structure exactly. The official client cannot read a layout it did not
   ask for.
3. Treat `implementation_observed` as a description of this codebase, not as a requirement. It is
   what we do; it may be wrong.
4. Treat a reference emulator the same way. Arcturus having done something for ten years is evidence
   that Arcturus does it.
5. Never convert an `unknown` into an assumption silently. If you must pick a behaviour to ship,
   write the choice into the spec's `verified:` block with `confidence: assumed` and say why.
6. Keep network serialisation out of domain logic and domain logic out of handlers — the flow
   recorded in each feature spec is the shape the specs expect to find.
7. Add or update a behavioural test when behaviour changes.
8. If you discover new evidence, put it in the tree (see below) and re-run `bootstrap`.

## Recording something you have learned

**From a capture** — the strongest thing you can add. Drop the JSON in
`docs/habbo-specs/evidence/captures/` (format in that directory's README) and run `bootstrap`. Every
scenario the capture touches stops being `unknown`, and the feature's emission ordering becomes
`strict` once repeated captures agree.

**From reading the client** — put it in the spec's `verified:` block, citing the class and line:

```yaml
verified:
  fields:
    - index: 3
      name: direction
      confidence: client_confirmed
      evidence: "WIN63-.../_SafeCls_3667.as:30 — the fourth constructor argument is the facing"
```

**Settling a conflict** — write into the conflict document's `verified:` block. Do not delete the
conflict: the next scan regenerates it from the same sources, and the resolution is what makes the
regeneration harmless.

`verified:` and `manual:` survive every regeneration. Editing inside `generated:` does not — the
digest catches it, the next `bootstrap` refuses to overwrite the file, and `validate` warns until it
is moved.

## Keeping the tree honest

```bash
dotnet run --project Vortex.Specs.Cli -- validate
```

Errors mean a claim outranks its evidence, an evidence id points at nothing, or a header id has
leaked into a behavioural spec. All three are ways for a spec to look more certain than it is.

## Comparing two runs

```bash
dotnet run --project Vortex.Specs.Cli -- diff official-capture.json vortex-capture.json
```

Aligns the two traces per trigger and reports missing, extra, reordered, wrong-recipient and
wrong-value packets. Capture the same action against Habbo and against this emulator and the output
is the work list.

## Regenerating

```bash
dotnet run --project Vortex.Specs.Cli -- bootstrap
```

Takes about a minute. An unchanged checkout produces a byte-identical tree, so a non-empty diff means
something really changed.

The scan reads sibling checkouts (`../*/sources/**`) for the official clients, Nitro and Arcturus. A
machine without them still produces a tree — with lower confidence, which the report says out loud.
