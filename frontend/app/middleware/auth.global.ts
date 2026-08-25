export default defineNuxtRouteMiddleware((to) => {
  if (import.meta.server) {
    return;
  }

  const { accessToken, currentUser, isInternalRole, isSupplierRole, homePathForRole } = useAuth();

  const isPublicRoute = to.path === "/login";

  if (!accessToken.value && !isPublicRoute) {
    return navigateTo("/login");
  }

  if (accessToken.value && isPublicRoute) {
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
