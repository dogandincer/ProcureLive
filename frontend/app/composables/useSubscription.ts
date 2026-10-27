import type { MySubscriptionDto } from "~/types/api";

export const SUBSCRIPTION_STATE_KEY = "mySubscription";

/**
 * Shared plan/quota state for the supplier side: the layout badge, the quota banner on the auction list,
 * the pricing page and the search gate all read the same state, so a purchase or a join refreshes all of them.
 * This only drives presentation — the backend enforces the quota and masks restricted data on its own.
 */
export function useSubscription() {
  const api = useApi();
  const subscription = useState<MySubscriptionDto | null>(SUBSCRIPTION_STATE_KEY, () => null);
  const loading = useState<boolean>(`${SUBSCRIPTION_STATE_KEY}Loading`, () => false);

  async function refresh(): Promise<MySubscriptionDto | null> {
    loading.value = true;
    try {
      subscription.value = await api.get<MySubscriptionDto>("/api/subscription/me");
    } catch {
      // badge/banner are best-effort; pages that depend on the plan show their own error
    } finally {
      loading.value = false;
    }
    return subscription.value;
  }

  const isPlus = computed(() => subscription.value?.isPlus ?? false);
  const quotaReached = computed(
    () => !!subscription.value && !subscription.value.isPlus && (subscription.value.remainingJoins ?? 0) <= 0,
  );

  return { subscription, loading, isPlus, quotaReached, refresh };
}
