<script setup lang="ts">
import type { AuctionDetailDto, AuctionDto, PagedResult } from "~/types/api";

const api = useApi();
const { currentUser } = useAuth();

const auctions = ref<AuctionDto[]>([]);
const myInvitationStatus = reactive<Record<string, string>>({});
const declineReason = reactive<Record<string, string>>({});
const showDeclineFor = ref<string | null>(null);

const statusBadge: Record<string, string> = {
  Draft: "bg-slate-100 text-slate-600",
  Scheduled: "bg-amber-100 text-amber-700",
  Live: "bg-emerald-100 text-emerald-700",
  Ended: "bg-slate-100 text-slate-600",
  Awarded: "bg-indigo-100 text-indigo-700",
  Cancelled: "bg-rose-100 text-rose-700",
};

async function load() {
  const page = await api.get<PagedResult<AuctionDto>>("/api/auctions", { page: 1, pageSize: 50 });
  auctions.value = page.items;

  for (const a of page.items) {
    try {
      const detail = await api.get<AuctionDetailDto>(`/api/auctions/${a.id}`);
      const inv = detail.invitations.find((i) => i.companyId === currentUser.value?.companyId);
      if (inv) myInvitationStatus[a.id] = inv.status;
    } catch {
      // ignore individual load failures, keep listing the rest
    }
  }
}

async function accept(auctionId: string) {
  await api.post(`/api/auctions/${auctionId}/invitations/respond`, { accept: true, declineReason: null });
  await load();
}

async function decline(auctionId: string) {
  await api.post(`/api/auctions/${auctionId}/invitations/respond`, {
    accept: false,
    declineReason: declineReason[auctionId] || "Belirtilmedi",
  });
  showDeclineFor.value = null;
  await load();
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <h1 class="text-xl font-bold text-slate-800">İhale Davetlerim</h1>

    <div class="rounded-xl border border-slate-200 bg-white p-5">
      <table class="w-full text-sm">
        <thead>
          <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
            <th class="pb-2">Başlık</th>
            <th class="pb-2">Alıcı</th>
            <th class="pb-2">Durum</th>
            <th class="pb-2">Davet Durumum</th>
            <th class="pb-2">Bitiş</th>
            <th class="pb-2"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="a in auctions" :key="a.id" class="border-b border-slate-50">
            <td class="py-2 font-medium text-slate-700">{{ a.title }}</td>
            <td class="py-2 text-slate-500">{{ a.ownerCompanyName }}</td>
            <td class="py-2"><span class="rounded px-2 py-0.5 text-xs font-medium" :class="statusBadge[a.status]">{{ a.status }}</span></td>
            <td class="py-2">
              <span class="rounded bg-slate-100 px-2 py-0.5 text-xs text-slate-600">{{ myInvitationStatus[a.id] ?? "-" }}</span>
            </td>
            <td class="py-2 text-slate-500">{{ new Date(a.endAt).toLocaleString("tr-TR") }}</td>
            <td class="py-2 text-right">
              <div v-if="myInvitationStatus[a.id] === 'Invited'" class="flex justify-end gap-2">
                <button class="rounded bg-emerald-600 px-2 py-1 text-xs font-semibold text-white" @click="accept(a.id)">Kabul Et</button>
                <button class="rounded bg-rose-600 px-2 py-1 text-xs font-semibold text-white" @click="showDeclineFor = a.id">Reddet</button>
              </div>
              <NuxtLink v-else :to="`/supplier/auctions/${a.id}`" class="text-indigo-600 hover:underline">Detay</NuxtLink>

              <div v-if="showDeclineFor === a.id" class="mt-2 flex justify-end gap-2">
                <input v-model="declineReason[a.id]" placeholder="Red gerekçesi" class="w-40 rounded-md border border-slate-300 px-2 py-1 text-xs" />
                <button class="rounded bg-rose-600 px-2 py-1 text-xs font-semibold text-white" @click="decline(a.id)">Onayla</button>
              </div>
            </td>
          </tr>
          <tr v-if="auctions.length === 0"><td colspan="6" class="py-6 text-center text-slate-400">Davet yok</td></tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
