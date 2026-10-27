export default defineNuxtRouteMiddleware((to) => {
  if (import.meta.server) {
    return;
  }

  const { accessToken, currentUser, isInternalRole, isSupplierRole, homePathForRole } = useAuth();

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

  if (to.path.startsWith("/supplier") && !isSupplierRole()) {
    return navigateTo(homePathForRole());
  }
});
