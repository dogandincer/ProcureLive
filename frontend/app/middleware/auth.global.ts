export default defineNuxtRouteMiddleware((to) => {
  if (import.meta.server) {
    return;
  }

  const { accessToken, currentUser, isInternalRole, isSupplierRole, isAdminRole, homePathForRole } = useAuth();

  // Giriş ve davetsiz tedarikçi kaydı oturumsuz açılır; oturum varken panele yönlenir.
  const isLoginRoute = to.path === "/login" || to.path === "/register";
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

  // Firma, İşyeri, Kullanıcı Tanım ve Yetki ekranları yalnızca Admin'e açık (sunucu da RequireAdmin ile korur).
  const adminOnly = ["/admin/firms", "/admin/workspaces", "/admin/users", "/admin/permissions"];
  if (adminOnly.some((p) => to.path.startsWith(p)) && !isAdminRole()) {
    return navigateTo("/admin");
  }

  if (to.path.startsWith("/supplier") && !isSupplierRole()) {
    return navigateTo(homePathForRole());
  }
});
