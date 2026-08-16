<script>
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { deniedRoute } from '../lib/session.js';
  import { NAV } from '../lib/routes.js';
  import { t } from '../lib/i18n.js';

  // The router lands here either after a failed capability guard (deniedRoute is set) or on an
  // unknown hash (deniedRoute empty -> treat as a missing section).
  let label = $derived($t(NAV.find((item) => item.path === $deniedRoute)?.labelKey || ''));
  let title = $derived($deniedRoute ? $t('accessDeniedPage.insufficientRightsTitle') : $t('accessDeniedPage.unknownRouteTitle'));
  let message = $derived($deniedRoute
    ? $t('accessDeniedPage.roleDenied', { section: label || $deniedRoute })
    : $t('accessDeniedPage.unknownSection'));
</script>

<AccessDeniedNotice {title} {message} />
