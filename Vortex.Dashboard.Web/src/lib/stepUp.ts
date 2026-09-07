// The bridge between a write that was refused for want of a recent second factor and the dialog that
// collects one.
//
// The server marks the critical operations by capability -- minting currency, the staff roster, the
// console, a database backup -- and refuses them with `mfa_step_up_required` when the session has not
// proved a factor inside the configured window. That refusal arrives inside writeOps, which is a
// store and cannot render a dialog; the dialog is mounted once at the app root and cannot reach into
// a write in progress. This is the seam between them: writeOps awaits `requestStepUp()`, the modal
// resolves it, and the write retries itself.
//
// Off entirely unless the server asks: with `Vortex:Observability:DashboardStepUpMinutes` at its
// default of 0 nothing ever refuses, so nothing here ever runs.
import { writable } from 'svelte/store';

/** `null`, or the request the modal is currently showing. */
export const stepUpRequest = writable<{ open: boolean } | null>(null);

/** The dialog in flight: its promise, and the way to settle it when the modal answers. */
let pending: { promise: Promise<boolean>; settle: (ok: boolean) => void } | null = null;

/**
 * Ask the operator for a current second-factor code.
 *
 * @returns {Promise<boolean>} true once a code has been accepted, false if they dismissed the dialog.
 */
export function requestStepUp(): Promise<boolean> {
  // A second write refused while the dialog is already open joins the one in flight rather than
  // stacking a second dialog on top of it.
  if (pending) {
    return pending.promise;
  }

  let settle!: (ok: boolean) => void;
  const promise = new Promise<boolean>((resolve) => {
    settle = resolve;
  });

  pending = { promise, settle };
  stepUpRequest.set({ open: true });

  return promise;
}

/** Called by the modal: the code was accepted, or the operator gave up. */
export function resolveStepUp(succeeded: boolean): void {
  const current = pending;

  pending = null;
  stepUpRequest.set(null);
  current?.settle(succeeded === true);
}
