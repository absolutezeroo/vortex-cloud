// The behaviour every dialog surface owes a keyboard user, in one place: focus moves into the panel
// on open, Tab cycles inside it instead of escaping to the page behind, Escape dismisses, and focus
// returns to whatever opened it so dismissing does not drop the caret at the top of the document.
//
// Extracted when the edit drawer arrived, because the alternative was a second copy of a focus trap
// -- and a focus trap that only half the dialogs have is worse than one nobody wrote, since the two
// then drift.
//
// Call it at the top level of a component's <script>; it registers its own mount/destroy hooks.
//
//   let panel = $state();
//   useDialogBehaviour(() => panel, { onClose: close });
import { onDestroy, onMount } from 'svelte';

const FOCUSABLE =
  'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

export function useDialogBehaviour(getPanel, { onClose } = {}) {
  let previouslyFocused = null;

  // Read the focusable list on each keypress rather than caching it, so conditional fields -- a
  // duration input that only exists while "permanent" is unchecked -- stay in the cycle.
  function trapFocus(event) {
    const panel = getPanel();
    const focusable = Array.from(panel?.querySelectorAll(FOCUSABLE) ?? []).filter(
      (el) => el.offsetParent !== null || el === document.activeElement
    );

    if (focusable.length === 0) {
      event.preventDefault();
      return;
    }

    const first = focusable[0];
    const last = focusable[focusable.length - 1];

    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }

  function onKeydown(event) {
    if (event.key === 'Escape') {
      event.stopPropagation();
      onClose?.();
      return;
    }

    if (event.key === 'Tab') {
      trapFocus(event);
    }
  }

  onMount(() => {
    previouslyFocused = document.activeElement;

    // Focus the first control so the operator can start typing; falls back to the panel itself for a
    // dialog that is only text plus a close button.
    const panel = getPanel();
    const target = panel?.querySelector(FOCUSABLE) ?? panel;
    target?.focus?.();

    window.addEventListener('keydown', onKeydown, true);
  });

  onDestroy(() => {
    window.removeEventListener('keydown', onKeydown, true);
    previouslyFocused?.focus?.();
  });
}
