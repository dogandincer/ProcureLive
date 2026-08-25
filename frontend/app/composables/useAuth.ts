import type { AuthResult, CurrentUser } from "~/types/api";

const ACCESS_TOKEN_COOKIE = "pl_access_token";
const REFRESH_TOKEN_COOKIE = "pl_refresh_token";

export function useAuth() {
  const accessToken = useCookie<string | null>(ACCESS_TOKEN_COOKIE, {
    default: () => null,
    sameSite: "lax",
  });
  const refreshToken = useCookie<string | null>(REFRESH_TOKEN_COOKIE, {
    default: () => null,
    sameSite: "lax",
  });
  const currentUser = useState<CurrentUser | null>("currentUser", () => null);

  const isAuthenticated = computed(() => !!accessToken.value && !!currentUser.value);

  function isInternalRole(): boolean {
    const roles = currentUser.value?.roles ?? [];
    return roles.includes("Admin") || roles.includes("Buyer");
  }

  function isSupplierRole(): boolean {
    return currentUser.value?.roles.includes("Supplier") ?? false;
  }

  function isAdminRole(): boolean {
    return currentUser.value?.roles.includes("Admin") ?? false;
  }

  async function login(email: string, password: string): Promise<void> {
    const config = useRuntimeConfig();
    const result = await $fetch<AuthResult>("/api/auth/login", {
      baseURL: config.public.apiBaseUrl,
      method: "POST",
      body: { email, password },
    });

    accessToken.value = result.accessToken;
    refreshToken.value = result.refreshToken;
    currentUser.value = {
      userId: result.userId,
      email: result.email,
      firstName: result.firstName,
      lastName: result.lastName,
      roles: result.roles,
      companyId: result.companyId,
    };
  }

  async function fetchMe(): Promise<void> {
    if (!accessToken.value) {
      return;
    }

    const config = useRuntimeConfig();

    try {
      const user = await $fetch<CurrentUser>("/api/auth/me", {
        baseURL: config.public.apiBaseUrl,
        headers: { Authorization: `Bearer ${accessToken.value}` },
      });
      currentUser.value = user;
    } catch {
      accessToken.value = null;
      refreshToken.value = null;
      currentUser.value = null;
    }
  }

  function logout(): void {
    accessToken.value = null;
    refreshToken.value = null;
    currentUser.value = null;
    navigateTo("/login");
  }

  function homePathForRole(): string {
    if (isInternalRole()) {
      return "/admin";
    }

    if (isSupplierRole()) {
      return "/supplier";
    }

    return "/login";
  }

  return {
    accessToken,
    refreshToken,
    currentUser,
    isAuthenticated,
    isInternalRole,
    isSupplierRole,
    isAdminRole,
    login,
    logout,
    fetchMe,
    homePathForRole,
  };
}
