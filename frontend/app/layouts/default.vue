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
  MagnifyingGlassIcon,
  SparklesIcon,
  Cog6ToothIcon,
  BuildingOffice2Icon,
  ChevronDoubleLeftIcon,
  ChevronDoubleRightIcon,
  ShieldCheckIcon,
  SwatchIcon,
  UserCircleIcon,
  ShoppingCartIcon,
} from "@heroicons/vue/24/outline";
import type { FirmDto, WorkspaceDto } from "~/types/api";

const { currentUser, isInternalRole, isSupplierRole, isAdminRole, logout } = useAuth();
const api = useApi();
const { connect } = useNotificationHub();
const lastNotification = useLastNotification();
const { t, locale } = useI18n();
const { formatDateTime } = useDateFormat();
const route = useRoute();

// Sistem firma + işyeri bazlı: en az bir firma ve bir işyeri tanımlı olmadan (iç kullanıcı) panel kilitlenir;
// Admin önce firma, sonra işyeri tanımlamaya yönlendirilir (tanım ekranları yalnızca Admin'e açık).
const { workspaces, init: initWorkspace, reconcile } = useWorkspace();
const { firms, selectedFirmId, ensureSelection } = useFirm();
const contextLoaded = ref(false);
async function loadContext() {
  try {
    const [firmList, wsList] = await Promise.all([api.get<FirmDto[]>("/api/firms"), api.get<WorkspaceDto[]>("/api/workspaces")]);
    firms.value = firmList;
    workspaces.value = wsList;
    initWorkspace();
    ensureSelection();
    reconcile(selectedFirmId.value);
  } catch {
    // sessiz
  } finally {
    contextLoaded.value = true;
  }
}
const setupGate = computed<null | "firm" | "workspace">(() => {
  if (!isInternalRole() || !contextLoaded.value) return null;
  if (firms.value.length === 0 && !route.path.startsWith("/admin/firms")) return "firm";
  if (firms.value.length > 0 && workspaces.value.length === 0 && !route.path.startsWith("/admin/workspaces") && !route.path.startsWith("/admin/firms")) return "workspace";
  return null;
});

// Marka (varsayılan firmadan) ve masaüstünde daraltılabilir kenar çubuğu (tercih tarayıcıda saklanır).
const { branding, logoSrc, initials } = useBranding();
const SIDEBAR_KEY = "pl_sidebar_collapsed";
const collapsed = ref(false);
function toggleSidebar() {
  if (window.matchMedia("(min-width: 768px)").matches) {
    collapsed.value = !collapsed.value;
    try {
      localStorage.setItem(SIDEBAR_KEY, collapsed.value ? "1" : "0");
    } catch {
      // tercih saklanamadı
    }
  } else {
    sidebarOpen.value = !sidebarOpen.value;
  }
}

const notifications = ref<NotificationDto[]>([]);
const unreadCount = ref(0);
const showNotifications = ref(false);
const unreadOnly = ref(false);
const sidebarOpen = ref(false);

interface NavLink {
  to: string;
  labelKey: string;
  icon: Component;
  adminOnly?: boolean;
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
      { to: "/admin/firms", labelKey: "nav.firms", icon: BuildingOffice2Icon, adminOnly: true },
      { to: "/admin/workspaces", labelKey: "nav.workspaces", icon: BuildingOfficeIcon, adminOnly: true },
      { to: "/admin/companies", labelKey: "nav.companies", icon: BuildingOfficeIcon },
      { to: "/admin/supplier-scoreboard", labelKey: "nav.scoreboard", icon: StarIcon },
      { to: "/admin/users", labelKey: "nav.users", icon: UsersIcon, adminOnly: true },
      { to: "/admin/permissions", labelKey: "nav.permissions", icon: ShieldCheckIcon, adminOnly: true },
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
      { to: "/admin/orders", labelKey: "nav.orders", icon: ShoppingCartIcon },
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
      { to: "/supplier/orders", labelKey: "nav.myOrders", icon: ShoppingCartIcon },
    ],
  },
  {
    sectionKey: "nav.plusGroup",
    links: [
      { to: "/supplier/search", labelKey: "nav.plusSearch", icon: MagnifyingGlassIcon },
      { to: "/supplier/pricing", labelKey: "nav.pricing", icon: SparklesIcon },
      { to: "/supplier/settings", labelKey: "nav.companySettings", icon: Cog6ToothIcon },
    ],
  },
];

// Tedarikçi plan rozeti (Plus / Standart kota) — fiyat sayfası ve katılım sonrası aynı state yenilenir.
const { subscription, isPlus, refresh: refreshSubscription } = useSubscription();

// Kişisel ayarlar (görünüm) her rolde menünün sonunda.
const accountSection: NavSection = {
  sectionKey: "nav.accountGroup",
  links: [
    { to: "/profile", labelKey: "nav.profile", icon: UserCircleIcon },
    { to: "/settings", labelKey: "nav.settings", icon: SwatchIcon },
  ],
};

const navSections = computed<NavSection[]>(() => {
  if (!isInternalRole()) return [...supplierSections, accountSection];
  // Firma ve İşyeri tanımları yalnızca Admin'e görünür.
  if (!isAdminRole()) return [...adminSections.map((s) => ({ ...s, links: s.links.filter((l) => !l.adminOnly) })), accountSection];
  return [
    ...adminSections,
    {
      sectionKey: "nav.systemGroup",
      links: [{ to: "/admin/audit-log", labelKey: "nav.auditLog", icon: ClipboardDocumentCheckIcon }],
    },
    accountSection,
  ];
});

const roleLabels: Record<string, string> = { Admin: "roles.admin", Buyer: "roles.buyer", Supplier: "roles.supplier" };
const roleDisplay = computed(() => currentUser.value?.roles.map((r) => t(roleLabels[r] ?? r)).join(", ") ?? "");

function closeSidebarOnNavigate() {
  sidebarOpen.value = false;
}

// Panel en yeni 15 bildirimle açılır; "Daha fazla göster" eski bildirimleri sayfa sayfa ekler.
const notificationPage = ref(1);
const notificationTotal = ref(0);
const loadingMoreNotifications = ref(false);
const NOTIFICATION_PAGE_SIZE = 15;
const hasMoreNotifications = computed(() => notifications.value.length < notificationTotal.value);

async function loadNotifications() {
  try {
    const [list, count] = await Promise.all([
      api.get<{ items: NotificationDto[]; totalCount: number }>("/api/notifications", { unreadOnly: unreadOnly.value || undefined, page: 1, pageSize: NOTIFICATION_PAGE_SIZE }),
      api.get<number>("/api/notifications/unread-count"),
    ]);
    notifications.value = list.items;
    notificationTotal.value = list.totalCount;
    notificationPage.value = 1;
    unreadCount.value = count;
  } catch {
    // layout should never crash the page over a notification fetch failure
  }
}

async function loadMoreNotifications() {
  loadingMoreNotifications.value = true;
  try {
    const next = notificationPage.value + 1;
    const list = await api.get<{ items: NotificationDto[]; totalCount: number }>("/api/notifications", {
      unreadOnly: unreadOnly.value || undefined, page: next, pageSize: NOTIFICATION_PAGE_SIZE,
    });
    const seen = new Set(notifications.value.map((n) => n.id));
    notifications.value = [...notifications.value, ...list.items.filter((n) => !seen.has(n.id))];
    notificationTotal.value = list.totalCount;
    notificationPage.value = next;
  } catch {
    // sessiz geç: panel kullanılabilir kalır
  } finally {
    loadingMoreNotifications.value = false;
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

// Bildirime tıklayınca ilgili kayda gidilir: iç kullanıcı yönetim ekranına, tedarikçi kendi ekranına.
function notificationLink(n: NotificationDto): string | null {
  if (!n.relatedEntityId) return null;
  const id = n.relatedEntityId;
  const internal = isInternalRole();
  switch (n.relatedEntityType) {
    case "Auction":
      return internal
        ? `/admin/auctions/${id}${n.type === "ProcessQuestion" ? "#qa" : ""}`
        : `/supplier/auctions/${id}${n.type === "ParticipationRequest" ? "#participation" : n.type === "ProcessQuestion" ? "#qa" : ""}`;
    case "RfiProcess":
      return (internal ? `/admin/rfi/${id}` : `/supplier/rfi/${id}`) + (n.type === "ProcessQuestion" ? "#qa" : "");
    case "RfqProcess":
      return (internal ? `/admin/rfq/${id}` : `/supplier/rfq/${id}`) + (n.type === "ProcessQuestion" ? "#qa" : "");
    case "Company":
      // Tedarikçiye gelen cari bildirimi (onay, abonelik) kendi paneline; belge süresi uyarısı belge kartına götürür.
      if (n.type === "DocumentExpiry") return internal ? `/admin/companies/${id}#documents` : "/supplier/settings#documents";
      return internal ? `/admin/companies/${id}` : "/supplier";
    case "PurchaseOrder":
      return internal ? `/admin/orders/${id}` : `/supplier/orders/${id}`;
    default:
      return null;
  }
}

// Panel; dışarı tıklanınca, odak panelden çıkınca veya Esc ile kapanır.
const bellRoot = ref<HTMLElement | null>(null);
const bellButton = ref<HTMLButtonElement | null>(null);
function onDocumentPointer(e: Event) {
  if (showNotifications.value && bellRoot.value && !bellRoot.value.contains(e.target as Node)) showNotifications.value = false;
}
function onBellFocusOut(e: FocusEvent) {
  const next = e.relatedTarget as Node | null;
  if (showNotifications.value && next && bellRoot.value && !bellRoot.value.contains(next)) showNotifications.value = false;
}
function onDocumentKey(e: KeyboardEvent) {
  if (e.key === "Escape" && showNotifications.value) {
    showNotifications.value = false;
    bellButton.value?.focus();
  }
}
onMounted(() => {
  document.addEventListener("pointerdown", onDocumentPointer, true);
  document.addEventListener("keydown", onDocumentKey);
});
onBeforeUnmount(() => {
  document.removeEventListener("pointerdown", onDocumentPointer, true);
  document.removeEventListener("keydown", onDocumentKey);
});

async function openNotification(n: NotificationDto) {
  await markRead(n);
  const link = notificationLink(n);
  if (link) {
    showNotifications.value = false;
    await navigateTo(link);
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
  try {
    collapsed.value = localStorage.getItem(SIDEBAR_KEY) === "1";
  } catch {
    collapsed.value = false;
  }
  if (isInternalRole()) loadContext();
  if (isSupplierRole()) refreshSubscription();
  await loadNotifications();

  await connect((notification) => {
    notifications.value = [notification, ...notifications.value].slice(0, 10);
    unreadCount.value += 1;
    // Açık sayfalar (ihale detayı, talep paneli) ilgili kayıt için gelen bildirimle kendini yenileyebilir.
    lastNotification.value = notification;
  });
});
</script>

<template>
  <div class="flex min-h-screen bg-slate-100">
    <!-- Mobile overlay -->
    <div
      v-if="sidebarOpen"
      class="fixed inset-0 z-30 bg-black/60 backdrop-blur-sm md:hidden"
      @click="sidebarOpen = false"
    />

    <!-- Sidebar -->
    <aside
      class="fixed inset-y-0 left-0 z-40 flex w-64 shrink-0 -translate-x-full flex-col bg-side-900 transition-all md:sticky md:top-0 md:h-screen md:translate-x-0"
      :class="[sidebarOpen ? 'translate-x-0' : '', collapsed ? 'md:w-16' : 'md:w-64']"
    >
      <!-- Brand (varsayılan firmanın logosu / adı) -->
      <div class="flex items-center gap-2.5 border-b border-side-700/60 py-4" :class="collapsed ? 'px-5 md:justify-center md:px-0' : 'px-5'">
        <div class="flex h-8 w-8 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-indigo-600 text-sm font-bold text-white">
          <img v-if="logoSrc" :src="logoSrc" alt="" class="h-full w-full bg-white object-contain" />
          <template v-else>{{ initials }}</template>
        </div>
        <div class="min-w-0" :class="{ 'md:hidden': collapsed }">
          <div class="truncate text-sm font-semibold leading-none text-side-50" :title="branding.appName">{{ branding.appName }}</div>
          <div class="mt-0.5 truncate text-[10px] text-side-400">{{ $t("app.tagline") }}</div>
        </div>
      </div>

      <div v-if="isInternalRole()" class="flex flex-col gap-2 border-b border-side-700/60 p-3 md:hidden">
        <FirmSwitcher block />
        <WorkspaceSwitcher block />
      </div>

      <!-- Nav -->
      <nav class="flex-1 overflow-y-auto py-3 px-2 space-y-5">
        <div v-for="(group, i) in navSections" :key="i">
          <div
            v-if="group.sectionKey"
            class="mb-1.5 px-3 text-[10px] font-semibold uppercase tracking-widest text-side-500"
            :class="{ 'md:hidden': collapsed }"
          >
            {{ $t(group.sectionKey) }}
          </div>
          <div class="flex flex-col gap-0.5">
            <NuxtLink
              v-for="link in group.links"
              :key="link.to"
              :to="link.to"
              class="group flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-side-300 hover:bg-side-800 hover:text-side-50 transition-colors"
              :class="{ 'md:justify-center md:px-0': collapsed }"
              :title="collapsed ? $t(link.labelKey) : undefined"
              active-class="bg-indigo-600 text-white shadow-sm"
              @click="closeSidebarOnNavigate"
            >
              <component :is="link.icon" class="h-4.5 w-4.5 shrink-0 opacity-80 group-[.router-link-active]:opacity-100" style="height:18px;width:18px" />
              <span class="flex-1" :class="{ 'md:hidden': collapsed }">{{ $t(link.labelKey) }}</span>
              <ChevronRightIcon class="h-3 w-3 shrink-0 opacity-0 group-hover:opacity-40 transition-opacity" :class="{ 'md:hidden': collapsed }" />
            </NuxtLink>
          </div>
        </div>
      </nav>

      <!-- Daralt / genişlet (masaüstü) -->
      <button
        class="hidden items-center gap-2 border-t border-side-700/60 px-5 py-2 text-xs text-side-400 hover:bg-side-800 hover:text-side-50 md:flex"
        :class="{ 'justify-center px-0': collapsed }"
        :title="collapsed ? $t('nav.expand') : $t('nav.collapse')"
        @click="toggleSidebar"
      >
        <ChevronDoubleRightIcon v-if="collapsed" class="h-4 w-4" />
        <template v-else><ChevronDoubleLeftIcon class="h-4 w-4" />{{ $t("nav.collapse") }}</template>
      </button>

      <!-- User info at bottom -->
      <div class="border-t border-side-700/60 px-4 py-3" :class="{ 'md:hidden': collapsed }">
        <div class="text-xs text-side-400 truncate">
          {{ currentUser?.firstName }} {{ currentUser?.lastName }}
        </div>
        <div class="mt-0.5 text-[10px] text-side-500">{{ roleDisplay }}</div>
        <NuxtLink
          v-if="isSupplierRole() && subscription"
          to="/supplier/pricing"
          class="mt-2 inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[10px] font-semibold"
          :class="isPlus ? 'bg-amber-400/20 text-amber-300' : 'bg-side-700 text-side-300 hover:bg-side-600'"
          @click="closeSidebarOnNavigate"
        >
          <SparklesIcon v-if="isPlus" class="h-3 w-3" />
          <template v-if="isPlus">Plus</template>
          <template v-else>{{ $t("subscription.badgeStandard", { used: subscription.joinedCount, limit: subscription.quotaLimit }) }}</template>
        </NuxtLink>
      </div>
    </aside>

    <!-- Main content -->
    <div class="flex min-h-screen min-w-0 flex-1 flex-col">
      <!-- Header -->
      <header class="sticky top-0 z-30 flex h-14 items-center justify-between gap-3 border-b border-slate-200 bg-surface px-4 sm:px-6 shadow-sm">
        <div class="flex min-w-0 flex-1 items-center gap-3">
          <!-- Hamburger: mobilde menüyü açar, masaüstünde kenar çubuğunu daraltır/genişletir -->
          <button
            class="shrink-0 rounded-md p-1.5 text-slate-500 hover:bg-slate-100"
            :title="$t('nav.toggleMenu')"
            @click="toggleSidebar"
          >
            <Bars3Icon class="h-5 w-5" />
          </button>
          <div class="hidden min-w-0 truncate text-sm font-medium text-slate-400 sm:block">
            {{ branding.appName }}
          </div>
        </div>

        <div class="flex shrink-0 items-center gap-1 sm:gap-3">
          <!-- Firma/işyeri seçimi: masaüstünde başlıkta, mobilde (yer olmadığı için) açılır menünün içinde -->
          <div v-if="isInternalRole()" class="hidden items-center gap-3 md:flex">
            <FirmSwitcher />
            <WorkspaceSwitcher />
          </div>
          <LanguageSwitcher />

          <!-- Notifications bell -->
          <div ref="bellRoot" class="relative" @focusout="onBellFocusOut">
            <button
              ref="bellButton"
              class="relative rounded-lg p-2 text-slate-500 hover:bg-slate-100 transition-colors"
              :aria-label="$t('notifications.title')"
              :aria-expanded="showNotifications"
              @click="showNotifications = !showNotifications"
            >
              <BellIcon class="h-5 w-5" />
              <span
                v-if="unreadCount > 0"
                class="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-rose-500 px-1 text-[10px] font-bold text-white ring-2 ring-surface"
              >
                {{ unreadCount > 99 ? "99+" : unreadCount }}
              </span>
            </button>

            <div
              v-if="showNotifications"
              class="absolute right-0 z-20 mt-2 w-80 max-w-[calc(100vw-2rem)] rounded-xl border border-slate-200 bg-surface shadow-xl"
              data-testid="notification-panel"
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
                  class="flex w-full items-start gap-2 border-b border-slate-50 px-4 py-3 text-left text-sm hover:bg-slate-50 transition-colors"
                  :class="{ 'bg-indigo-50/60': !n.isRead }"
                  :title="notificationLink(n) ? $t('notifications.openRecord') : undefined"
                  data-testid="notification-item"
                  @click="openNotification(n)"
                >
                  <span class="min-w-0 flex-1">
                    <span class="block font-medium leading-snug text-slate-800">{{ n.title }}</span>
                    <span class="mt-0.5 line-clamp-2 block text-xs text-slate-500">{{ n.body }}</span>
                    <span class="mt-1 block text-[11px] text-slate-400">{{ formatDateTime(n.createdAt) }}</span>
                  </span>
                  <ChevronRightIcon v-if="notificationLink(n)" class="mt-0.5 h-4 w-4 shrink-0 text-slate-400" />
                </button>
                <button
                  v-if="hasMoreNotifications"
                  type="button"
                  class="block w-full px-4 py-2.5 text-center text-xs font-medium text-indigo-600 hover:bg-slate-50 disabled:opacity-60"
                  :disabled="loadingMoreNotifications"
                  data-testid="notifications-more"
                  @click="loadMoreNotifications"
                >
                  {{ loadingMoreNotifications ? $t("common.loading") : $t("notifications.loadMore", { shown: notifications.length, total: notificationTotal }) }}
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

      <main class="flex-1 overflow-x-clip p-4 sm:p-6">
        <div v-if="setupGate" class="flex min-h-[60vh] items-center justify-center">
          <div class="max-w-md rounded-2xl border border-slate-200 bg-surface p-8 text-center shadow-sm">
            <div class="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-indigo-50">
              <BuildingOffice2Icon v-if="setupGate === 'firm'" class="h-6 w-6 text-indigo-600" />
              <BuildingOfficeIcon v-else class="h-6 w-6 text-indigo-600" />
            </div>
            <h2 class="text-lg font-bold text-slate-800">{{ isAdminRole() ? $t(`workspaceGate.${setupGate}.title`) : $t("workspaceGate.noAccessTitle") }}</h2>
            <p class="mt-2 text-sm text-slate-500">
              {{ isAdminRole() ? $t(`workspaceGate.${setupGate}.message`) : $t("workspaceGate.noAccess") }}
            </p>
            <NuxtLink
              v-if="isAdminRole()"
              :to="setupGate === 'firm' ? '/admin/firms' : '/admin/workspaces'"
              class="mt-5 inline-flex items-center gap-2 rounded-lg bg-indigo-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-indigo-700"
            >
              {{ $t(`workspaceGate.${setupGate}.cta`) }}
            </NuxtLink>
          </div>
        </div>
        <slot v-else />
      </main>
    </div>
  </div>
</template>
