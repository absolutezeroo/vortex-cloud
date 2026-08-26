# Classification policy

## MISSING

Target obligation has no usable Vortex entry path.

## PARTIAL

Entry exists but meaningful end-to-end behaviour is absent or not complete.

## IMPLEMENTED

Meaningful Vortex path exists. This is an implementation statement only.

## COMPLETE

Implemented plus evidence-backed verification appropriate to the risk and protocol claim.

## UNKNOWN

Sources do not support a safe classification.

## NOT_APPLICABLE

Explicit, evidence-backed reachability/product exclusion.

## UNRESOLVED_SURFACE

Target-client candidate cannot yet be bound sufficiently to score.

### Promotion rules

```text
MISSING -> PARTIAL
requires actual entry path

PARTIAL -> IMPLEMENTED
requires meaningful domain flow

IMPLEMENTED -> COMPLETE
requires verification evidence

anything -> NOT_APPLICABLE
requires explicit reason/evidence/decision

MISSING -> COMPLETE
forbidden
```
