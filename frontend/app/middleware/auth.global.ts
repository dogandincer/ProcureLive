export default defineNuxtRouteMiddleware((to) => {
  if (import.meta.server) {
    return;
  }

  const { accessToken, currentUser, isInternalRole, isSupplierRole, isAdminRole, homePathForRole } = useAuth();

  const isLoginRoute = to.path === "/login";
  // Davet linki hem oturumsuz (kayıt/giriş) hem oturumlu (sahiplenme) açılabilmeli.
  const isInviteRoute = to.path === "/invite";

  if (isInviteRoute) {
    return;
  }

  if (!accessToken.value && !isLoginRoute) {
    return navigateTo("/login");
  }

  if (accessToken.value && isLoginRoute) {
    return navigateTo(homePathForRole());
  }

  if (!currentUser.value) {
    // Still hydrating (auth.client plugin runs first render); allow through, per-page guards recheck.
    return;
  }

  if (to.path.startsWith("/admin") && !isInternalRole()) {
    return navigateTo(homePathForRole());
  }

  // Firma ve İşyeri tanım ekranları yalnızca Admin'e açık (sunucu da RequireAdmin ile korur).
  if ((to.path.startsWith("/admin/firms") || to.path.startsWith("/admin/workspaces")) && !isAdminRole()) {
    return navigateTo("/admin");
  }

  if (to.path.startsWith("/supplier") && !isSupplierRole()) {
    return navigateTo(homePathForRole());
  }
});
