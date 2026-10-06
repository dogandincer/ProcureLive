<script setup lang="ts">
import type { Component } from "vue";
import type { NotificationDto } from "~/types/api";
import {
  HomeIcon,
  BuildingOfficeIcon,
  UsersIcon,
  CubeIcon,
  TagIcon,
  BanknotesIcon,
  ClipboardDocumentListIcon,
  ArchiveBoxIcon,
  EnvelopeOpenIcon,
  QuestionMarkCircleIcon,
  DocumentTextIcon,
  ClipboardDocumentCheckIcon,
  ChartBarIcon,
  StarIcon,
  Bars3Icon,
  BellIcon,
  ArrowRightOnRectangleIcon,
  ChevronRightIcon,
} from "@heroicons/vue/24/outline";

const { currentUser, isInternalRole, isSupplierRole, isAdminRole, logout } = useAuth();
const api = useApi();
const { connect } = useNotificationHub();
const { t, locale } = useI18n();

const notifications = ref<NotificationDto[]>([]);
const unreadCount = ref(0);
const showNotifications = ref(false);
const unreadOnly = ref(false);
const sidebarOpen = ref(false);

interface NavLink {
  to: string;
  labelKey: string;
  icon: Component;
}

interface NavSection {
  sectionKey: string | null;
  links: NavLink[];
}

const adminSections: NavSection[] = [
  { sectionKey: null, links: [{ to: "/admin", labelKey: "nav.panel", icon: HomeIcon }] },
  {
    sectionKey: "nav.supplyChainGroup",
    links: [
      { to: "/admin/workspaces", labelKey: "nav.workspaces", icon: BuildingOfficeIcon },
      { to: "/admin/companies", labelKey: "nav.companies", icon: BuildingOfficeIcon },
      { to: "/admin/supplier-scoreboard", labelKey: "nav.scoreboard", icon: StarIcon },
      { to: "/admin/users", labelKey: "nav.users", icon: UsersIcon },
    ],
  },
  {
    sectionKey: "nav.catalogGroup",
    links: [
      { to: "/admin/products", labelKey: "nav.products", icon: CubeIcon },
      { to: "/admin/categories", labelKey: "nav.categories", icon: TagIcon },
      { to: "/admin/sectors", labelKey: "nav.sectors", icon: BuildingOfficeIcon },
      { to: "/admin/currencies", labelKey: "nav.currencies", icon: BanknotesIcon },
    ],
  },
  {
    sectionKey: "nav.processesGroup",
    links: [
      { to: "/admin/auctions", labelKey: "nav.auctions", icon: ClipboardDocumentListIcon },
      { to: "/admin/rfi", labelKey: "nav.rfi", icon: QuestionMarkCircleIcon },
      { to: "/admin/rfq", labelKey: "nav.rfq", icon: DocumentTextIcon },
      { to: "/admin/monitoring", labelKey: "nav.monitoring", icon: ChartBarIcon },
    ],
  },
];

const supplierSections: NavSection[] = [
  {
    sectionKey: null,
    links: [
      { to: "/supplier", labelKey: "nav.myPanel", icon: HomeIcon },
      { to: "/supplier/auctions", labelKey: "nav.auctionInvites", icon: EnvelopeOpenIcon },
      { to: "/supplier/history", labelKey: "nav.auctionHistory", icon: ArchiveBoxIcon },
      { to: "/supplier/rfi", labelKey: "nav.rfiInvites", icon: QuestionMarkCircleIcon },
      { to: "/supplier/rfq", labelKey: "nav.rfqInvites", icon: DocumentTextIcon },
    ],
  },
];

const navSections = computed<NavSection[]>(() => {
  if (!isInternalRole()) return supplierSections;
  if (!isAdminRole()) return adminSections;
  return [
    ...adminSections,
    {
      sectionKey: "nav.systemGroup",
      links: [{ to: "/admin/audit-log", labelKey: "nav.auditLog", icon: ClipboardDocumentCheckIcon }],
    },
  ];
});

const roleLabels: Record<string, string> = { Admin: "roles.admin", Buyer: "roles.buyer", Supplier: "roles.supplier" };
const roleDisplay = computed(() => currentUser.value?.roles.map((r) => t(roleLabels[r] ?? r)).join(", ") ?? "");

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
    // best-effort
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
    // best-effort
  }
}

watch(unreadOnly, loadNotifications);

// Notification title/body are translated server-side per the request's Accept-Language, so a
// language switch needs a fresh fetch to re-render already-loaded notifications in the new language.
watch(locale, loadNotifications);

onMounted(async () => {
  await loadNotifications();

  await connect((notification) => {
    notifications.value = [notification, ...notifications.value].slice(0, 10);
    unreadCount.value += 1;
  });
});
</script>

<template>
  <div class="flex min-h-screen bg-slate-100">
    <!-- Mobile overlay -->
    <div
      v-if="sidebarOpen"
      class="fixed inset-0 z-30 bg-slate-900/60 backdrop-blur-sm md:hidden"
      @click="sidebarOpen = false"
    />

    <!-- Sidebar -->
    <aside
      class="fixed inset-y-0 left-0 z-40 flex w-64 shrink-0 -translate-x-full flex-col bg-slate-900 transition-transform md:static md:translate-x-0"
      :class="{ 'translate-x-0': sidebarOpen }"
    >
      <!-- Brand -->
      <div class="flex items-center gap-2.5 px-5 py-4 border-b border-slate-700/60">
        <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-600 text-white text-sm font-bold shrink-0">
          PL
        </div>
        <div>
          <div class="text-sm font-semibold text-white leading-none">{{ $t("app.name") }}</div>
          <div class="text-[10px] text-slate-400 mt-0.5">{{ $t("app.tagline") }}</div>
        </div>
      </div>

      <!-- Nav -->
      <nav class="flex-1 overflow-y-auto py-3 px-2 space-y-5">
        <div v-for="(group, i) in navSections" :key="i">
          <div
            v-if="group.sectionKey"
            class="mb-1.5 px-3 text-[10px] font-semibold uppercase tracking-widest text-slate-500"
          >
            {{ $t(group.sectionKey) }}
          </div>
          <div class="flex flex-col gap-0.5">
            <NuxtLink
              v-for="link in group.links"
              :key="link.to"
              :to="link.to"
              class="group flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-slate-300 hover:bg-slate-800 hover:text-white transition-colors"
              active-class="bg-indigo-600 text-white shadow-sm"
              @click="closeSidebarOnNavigate"
            >
              <component :is="link.icon" class="h-4.5 w-4.5 shrink-0 opacity-80 group-[.router-link-active]:opacity-100" style="height:18px;width:18px" />
              <span class="flex-1">{{ $t(link.labelKey) }}</span>
              <ChevronRightIcon class="h-3 w-3 shrink-0 opacity-0 group-hover:opacity-40 transition-opacity" />
            </NuxtLink>
          </div>
        </div>
      </nav>

      <!-- User info at bottom -->
      <div class="border-t border-slate-700/60 px-4 py-3">
        <div class="text-xs text-slate-400 truncate">
          {{ currentUser?.firstName }} {{ currentUser?.lastName }}
        </div>
        <div class="mt-0.5 text-[10px] text-slate-500">{{ roleDisplay }}</div>
      </div>
    </aside>

    <!-- Main content -->
    <div class="flex min-h-screen min-w-0 flex-1 flex-col">
      <!-- Header -->
      <header class="flex items-center justify-between gap-3 border-b border-slate-200 bg-white px-4 py-2.5 sm:px-6 shadow-sm">
        <div class="flex min-w-0 flex-1 items-center gap-3">
          <!-- Mobile hamburger -->
          <button
            class="shrink-0 rounded-md p-1.5 text-slate-500 hover:bg-slate-100 md:hidden"
            @click="sidebarOpen = true"
          >
            <Bars3Icon class="h-5 w-5" />
          </button>
          <!-- Page breadcrumb area (slot for pages to fill, or just empty) -->
          <div class="min-w-0 hidden sm:block text-sm text-slate-400 font-medium">
            {{ $t("app.name") }}
          </div>
        </div>

        <div class="flex shrink-0 items-center gap-1 sm:gap-3">
          <WorkspaceSwitcher v-if="isInternalRole()" />
          <LanguageSwitcher />

          <!-- Notifications bell -->
          <div class="relative">
            <button
              class="relative rounded-lg p-2 text-slate-500 hover:bg-slate-100 transition-colors"
              @click="showNotifications = !showNotifications"
            >
              <BellIcon class="h-5 w-5" />
              <span
                v-if="unreadCount > 0"
                class="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-rose-500 px-1 text-[10px] font-bold text-white ring-2 ring-white"
              >
                {{ unreadCount > 99 ? "99+" : unreadCount }}
              </span>
            </button>

            <div
              v-if="showNotifications"
              class="absolute right-0 z-20 mt-2 w-80 max-w-[calc(100vw-2rem)] rounded-xl border border-slate-200 bg-white shadow-xl"
            >
              <div class="flex items-center justify-between border-b border-slate-100 px-4 py-2.5">
                <span class="text-sm font-semibold text-slate-800">{{ $t("notifications.title") }}</span>
                <div class="flex items-center gap-3">
                  <label class="flex items-center gap-1.5 text-xs text-slate-500 cursor-pointer">
                    <input v-model="unreadOnly" type="checkbox" class="rounded" />
                    {{ $t("notifications.unreadOnly") }}
                  </label>
                  <button
                    v-if="unreadCount > 0"
                    class="text-xs font-medium text-indigo-600 hover:text-indigo-700"
                    @click="markAllRead"
                  >
                    {{ $t("notifications.markAllRead") }}
                  </button>
                </div>
              </div>
              <div class="max-h-96 overflow-y-auto">
                <p v-if="notifications.length === 0" class="px-4 py-6 text-center text-sm text-slate-400">
                  {{ unreadOnly ? $t("notifications.emptyUnread") : $t("notifications.empty") }}
                </p>
                <button
                  v-for="n in notifications"
                  :key="n.id"
                  class="block w-full border-b border-slate-50 px-4 py-3 text-left text-sm hover:bg-slate-50 transition-colors"
                  :class="{ 'bg-indigo-50/60': !n.isRead }"
                  @click="markRead(n)"
                >
                  <div class="font-medium text-slate-800 leading-snug">{{ n.title }}</div>
                  <div class="mt-0.5 line-clamp-2 text-xs text-slate-500">{{ n.body }}</div>
                </button>
              </div>
            </div>
          </div>

          <!-- Logout -->
          <button
            class="flex items-center gap-1.5 rounded-lg px-2.5 py-1.5 text-sm font-medium text-slate-500 hover:bg-rose-50 hover:text-rose-600 transition-colors"
            @click="logout"
          >
            <ArrowRightOnRectangleIcon class="h-4 w-4" />
            <span class="hidden sm:inline">{{ $t("common.logout") }}</span>
          </button>
        </div>
      </header>

      <main class="flex-1 overflow-x-hidden p-4 sm:p-6">
        <slot />
      </main>
    </div>
  </div>
</template>
