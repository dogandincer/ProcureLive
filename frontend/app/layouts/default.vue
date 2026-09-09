<script setup lang="ts">
import type { NotificationDto } from "~/types/api";

const { currentUser, isInternalRole, isSupplierRole, isAdminRole, logout } = useAuth();
const api = useApi();
const { connect } = useNotificationHub();

const notifications = ref<NotificationDto[]>([]);
const unreadCount = ref(0);
const showNotifications = ref(false);
const unreadOnly = ref(false);
const sidebarOpen = ref(false);

interface NavSection {
  section: string | null;
  links: { to: string; label: string }[];
}

const adminSections: NavSection[] = [
  { section: null, links: [{ to: "/admin", label: "Panel" }] },
  {
    section: "Tedarik Zinciri",
    links: [
      { to: "/admin/companies", label: "Cariler" },
      { to: "/admin/users", label: "Kullanıcılar" },
    ],
  },
  {
    section: "Katalog",
    links: [
      { to: "/admin/products", label: "Stok / Ürünler" },
      { to: "/admin/categories", label: "Kategoriler" },
      { to: "/admin/currencies", label: "Para Birimleri" },
    ],
  },
  {
    section: "Süreçler",
    links: [
      { to: "/admin/auctions", label: "İhaleler" },
      { to: "/admin/rfi", label: "RFI" },
      { to: "/admin/rfq", label: "RFQ" },
    ],
  },
];

const supplierSections: NavSection[] = [
  {
    section: null,
    links: [
      { to: "/supplier", label: "Panelim" },
      { to: "/supplier/auctions", label: "İhale Davetlerim" },
      { to: "/supplier/history", label: "İhale Geçmişim" },
      { to: "/supplier/rfi", label: "RFI Davetlerim" },
      { to: "/supplier/rfq", label: "RFQ Davetlerim" },
    ],
  },
];

const navSections = computed<NavSection[]>(() => {
  if (!isInternalRole()) return supplierSections;
  if (!isAdminRole()) return adminSections;
  return [...adminSections, { section: "Sistem", links: [{ to: "/admin/audit-log", label: "Denetim Kaydı" }] }];
});

function closeSidebarOnNavigate() {
  sidebarOpen.value = false;
}

async function loadNotifications() {
  try {
    const [list, count] = await Promise.all([
      api.get<{ items: NotificationDto[] }>("/api/notifications", { unreadOnly: unreadOnly.value || undefined, pageSize: 10 }),
      api.get<number>("/api/notifications/unread-count"),
    ]);
    notifications.value = list.items;
    unreadCount.value = count;
  } catch {
    // layout should never crash the page over a notification fetch failure
  }
}

async function markRead(notification: NotificationDto) {
  if (notification.isRead) {
    return;
  }
  try {
    await api.post(`/api/notifications/${notification.id}/read`);
    notification.isRead = true;
    unreadCount.value = Math.max(0, unreadCount.value - 1);
    if (unreadOnly.value) {
      notifications.value = notifications.value.filter((n) => n.id !== notification.id);
    }
  } catch {
    // best-effort: leave the notification marked unread so the user can retry by clicking again
  }
}

async function markAllRead() {
  try {
    await api.post("/api/notifications/read-all");
    unreadCount.value = 0;
    if (unreadOnly.value) {
      notifications.value = [];
    } else {
      notifications.value = notifications.value.map((n) => ({ ...n, isRead: true }));
    }
  } catch {
    // best-effort: leave state as-is so the user can retry by clicking again
  }
}

watch(unreadOnly, loadNotifications);

onMounted(async () => {
  await loadNotifications();

  await connect((notification) => {
    notifications.value = [notification, ...notifications.value].slice(0, 10);
    unreadCount.value += 1;
  });
});
</script>

<template>
  <div class="flex min-h-screen">
    <div
      v-if="sidebarOpen"
      class="fixed inset-0 z-30 bg-slate-900/30 md:hidden"
      @click="sidebarOpen = false"
    />

    <aside
      class="fixed inset-y-0 left-0 z-40 w-64 shrink-0 -translate-x-full border-r border-slate-200 bg-white transition-transform md:static md:translate-x-0"
      :class="{ 'translate-x-0': sidebarOpen }"
    >
      <div class="px-5 py-4 text-lg font-bold text-indigo-700">ProcureLive</div>
      <nav class="mt-2 flex flex-col gap-4 px-3">
        <div v-for="(group, i) in navSections" :key="i">
          <div v-if="group.section" class="mb-1 px-3 text-xs font-semibold uppercase tracking-wide text-slate-400">
            {{ group.section }}
          </div>
          <div class="flex flex-col gap-1">
            <NuxtLink
              v-for="link in group.links"
              :key="link.to"
              :to="link.to"
              class="rounded-md px-3 py-2 text-sm font-medium text-slate-700 hover:bg-indigo-50 hover:text-indigo-700"
              active-class="bg-indigo-50 text-indigo-700"
              @click="closeSidebarOnNavigate"
            >
              {{ link.label }}
            </NuxtLink>
          </div>
        </div>
      </nav>
    </aside>

    <div class="flex min-h-screen flex-1 flex-col">
      <header class="flex items-center justify-between border-b border-slate-200 bg-white px-4 py-3 sm:px-6">
        <div class="flex items-center gap-3">
          <button class="rounded-md p-1.5 text-slate-500 hover:bg-slate-100 md:hidden" @click="sidebarOpen = true">
            ☰
          </button>
          <div class="text-sm text-slate-500">
            {{ currentUser?.firstName }} {{ currentUser?.lastName }}
            <span class="ml-2 rounded bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600">
              {{ currentUser?.roles.join(", ") }}
            </span>
          </div>
        </div>

        <div class="flex items-center gap-4">
          <div class="relative">
            <button
              class="relative rounded-full p-2 text-slate-500 hover:bg-slate-100"
              @click="showNotifications = !showNotifications"
            >
              🔔
              <span
                v-if="unreadCount > 0"
                class="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-rose-600 px-1 text-[10px] font-bold text-white"
              >
                {{ unreadCount }}
              </span>
            </button>

            <div
              v-if="showNotifications"
              class="absolute right-0 z-20 mt-2 w-80 max-w-[calc(100vw-2rem)] rounded-lg border border-slate-200 bg-white shadow-lg"
            >
              <div class="flex items-center justify-between border-b border-slate-100 px-4 py-2">
                <span class="text-sm font-semibold">Bildirimler</span>
                <div class="flex items-center gap-3">
                  <label class="flex items-center gap-1 text-xs text-slate-500">
                    <input v-model="unreadOnly" type="checkbox" />
                    Okunmamış
                  </label>
                  <button
                    v-if="unreadCount > 0"
                    class="text-xs font-medium text-indigo-600 hover:underline"
                    @click="markAllRead"
                  >
                    Tümünü okundu işaretle
                  </button>
                </div>
              </div>
              <div class="max-h-96 overflow-y-auto">
                <p v-if="notifications.length === 0" class="px-4 py-6 text-center text-sm text-slate-400">
                  {{ unreadOnly ? "Okunmamış bildirim yok" : "Bildirim yok" }}
                </p>
                <button
                  v-for="n in notifications"
                  :key="n.id"
                  class="block w-full border-b border-slate-50 px-4 py-3 text-left text-sm hover:bg-slate-50"
                  :class="{ 'bg-indigo-50/50': !n.isRead }"
                  @click="markRead(n)"
                >
                  <div class="font-medium text-slate-800">{{ n.title }}</div>
                  <div class="mt-0.5 line-clamp-2 text-xs text-slate-500">{{ n.body }}</div>
                </button>
              </div>
            </div>
          </div>

          <button class="text-sm font-medium text-slate-500 hover:text-rose-600" @click="logout">Çıkış</button>
        </div>
      </header>

      <main class="flex-1 overflow-x-hidden bg-slate-50 p-4 sm:p-6">
        <slot />
      </main>
    </div>
  </div>
</template>
