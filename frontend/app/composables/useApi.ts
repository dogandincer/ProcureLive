/**
 * FluentValidation failures (400) come back as { title: "Doğrulama hatası.", errors: { Field: ["specific message"] } } -
 * the generic title alone tells the user nothing. Prefer the first field-level message when present.
 */
export function apiErrorMessage(error: any, fallback: string): string {
  const data = error?.data;
  const fieldErrors = data?.errors as Record<string, string[]> | undefined;
  if (fieldErrors) {
    const firstMessages = Object.values(fieldErrors).find((messages) => Array.isArray(messages) && messages.length > 0);
    if (firstMessages) return firstMessages[0];
  }
  return data?.title ?? data?.detail ?? fallback;
}

export function useApi() {
  const config = useRuntimeConfig();
  const { accessToken, logout } = useAuth();

  async function request<T>(path: string, options: Parameters<typeof $fetch>[1] = {}): Promise<T> {
    try {
      return await $fetch<T>(path, {
        baseURL: config.public.apiBaseUrl,
        ...options,
        headers: {
          ...(accessToken.value ? { Authorization: `Bearer ${accessToken.value}` } : {}),
          ...(options.headers as Record<string, string> | undefined),
        },
      });
    } catch (error: any) {
      if (error?.response?.status === 401) {
        logout();
      }
      throw error;
    }
  }

  return {
    get: <T>(path: string, query?: Record<string, unknown>) => request<T>(path, { method: "GET", query }),
    post: <T>(path: string, body?: unknown) => request<T>(path, { method: "POST", body }),
    put: <T>(path: string, body?: unknown) => request<T>(path, { method: "PUT", body }),
    del: <T>(path: string) => request<T>(path, { method: "DELETE" }),
  };
}
