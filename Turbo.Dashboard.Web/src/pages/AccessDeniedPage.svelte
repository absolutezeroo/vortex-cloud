<script>
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { deniedRoute } from '../lib/session.js';
  import { NAV } from '../lib/routes.js';

  // The router lands here either after a failed capability guard (deniedRoute is set) or on an
  // unknown hash (deniedRoute empty -> treat as a missing section).
  $: label = NAV.find((item) => item.path === $deniedRoute)?.label || '';
  $: title = $deniedRoute ? 'Droits insuffisants' : 'Route inconnue';
  $: message = $deniedRoute
    ? `Votre rôle ne permet pas d'accéder à ${label || $deniedRoute}.`
    : "Cette section n'existe pas.";
</script>

<AccessDeniedNotice {title} {message} />
