# Completeness bootstrap snapshot

**Target:** `WIN63-202607011411-782849652`  
**Status:** no completeness score is valid until FC-1 exists.

| Known source fact | Count |
|---|---:|
| target client packets | 1168 |
| target incoming | 580 |
| target outgoing | 588 |
| merged incoming PacketSpecs | 601 |
| merged outgoing PacketSpecs | 839 |
| Vortex-derived FeatureSpecs | 497 |
| scenarios | 1876 |
| captures | 0 |
| critical unknowns | 134 |
| unknowns total | 715 |
| conflicts | 365 |

`497 / 580` is explicitly invalid because features are built from Vortex flows.

`docs/codebase/` also records unreachable/unmapped implementation artifacts. Those become triage
signals after FC-1; they are not silently added to the packet denominator.
