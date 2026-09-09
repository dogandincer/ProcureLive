<script setup lang="ts">
import type { CompanyDetailDto } from "~/types/api";

const route = useRoute();
const companyId = route.params.id as string;
const api = useApi();
const toast = useToast();

const company = ref<CompanyDetailDto | null>(null);
const loading = ref(true);
const showContactForm = ref(false);
const showAddressForm = ref(false);

const contactForm = reactive({ firstName: "", lastName: "", title: "", email: "", phone: "", isPrimary: false });
const addressTypes = ["HeadOffice", "Billing", "Shipping"];
const addressForm = reactive({ addressType: "HeadOffice", country: "Türkiye", city: "", district: "", postalCode: "", line1: "", line2: "" });

async function load() {
  loading.value = true;
  try {
    company.value = await api.get<CompanyDetailDto>(`/api/companies/${companyId}`);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "Cari yüklenemedi."));
  } finally {
    loading.value = false;
  }
}

async function addContact() {
  try {
    await api.post(`/api/companies/${companyId}/contacts`, {
      firstName: contactForm.firstName,
      lastName: contactForm.lastName,
      title: contactForm.title || null,
      email: contactForm.email,
      phone: contactForm.phone || null,
      isPrimary: contactForm.isPrimary,
    });
    Object.assign(contactForm, { firstName: "", lastName: "", title: "", email: "", phone: "", isPrimary: false });
    showContactForm.value = false;
    toast.success("Yetkili kişi eklendi.");
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "Yetkili kişi eklenemedi."));
  }
}

async function addAddress() {
  try {
    await api.post(`/api/companies/${companyId}/addresses`, {
      addressType: addressForm.addressType,
      country: addressForm.country,
      city: addressForm.city,
      district: addressForm.district || null,
      postalCode: addressForm.postalCode || null,
      line1: addressForm.line1,
      line2: addressForm.line2 || null,
    });
    Object.assign(addressForm, { addressType: "HeadOffice", country: "Türkiye", city: "", district: "", postalCode: "", line1: "", line2: "" });
    showAddressForm.value = false;
    toast.success("Adres eklendi.");
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "Adres eklenemedi."));
  }
}

onMounted(load);
</script>

<template>
  <LoadingSpinner v-if="loading" />

  <div v-else-if="company" class="space-y-6">
    <div>
      <NuxtLink to="/admin/companies" class="text-xs text-indigo-600 hover:underline">← Cariler</NuxtLink>
      <h1 class="mt-1 text-xl font-bold text-slate-800">{{ company.name }}</h1>
      <p class="text-sm text-slate-500">{{ company.taxNumber }} · {{ company.taxOffice }} · {{ company.companyType }} · {{ company.status }}</p>
    </div>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <AppCard>
        <div class="flex items-center justify-between">
          <h2 class="text-sm font-semibold text-slate-700">Yetkili Kişiler</h2>
          <button class="text-xs font-medium text-indigo-600 hover:underline" @click="showContactForm = !showContactForm">
            {{ showContactForm ? "Vazgeç" : "+ Yeni" }}
          </button>
        </div>

        <form v-if="showContactForm" class="mt-3 grid grid-cols-2 gap-2" @submit.prevent="addContact">
          <input v-model="contactForm.firstName" placeholder="Ad" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="contactForm.lastName" placeholder="Soyad" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="contactForm.title" placeholder="Unvan (opsiyonel)" class="col-span-2 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="contactForm.email" type="email" placeholder="E-posta" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="contactForm.phone" placeholder="Telefon (opsiyonel)" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <label class="col-span-2 flex items-center gap-2 text-xs text-slate-600">
            <input v-model="contactForm.isPrimary" type="checkbox" />
            Birincil iletişim kişisi
          </label>
          <button type="submit" class="col-span-2 rounded-md bg-slate-800 px-3 py-1.5 text-sm font-semibold text-white">Kaydet</button>
        </form>

        <ul v-if="company.contactPersons.length > 0" class="mt-3 space-y-2 text-sm">
          <li v-for="c in company.contactPersons" :key="c.id" class="rounded-lg border border-slate-100 p-2">
            <div class="flex items-center justify-between">
              <span class="font-medium text-slate-700">{{ c.firstName }} {{ c.lastName }}</span>
              <AppBadge v-if="c.isPrimary" tone="indigo">Birincil</AppBadge>
            </div>
            <div class="text-xs text-slate-500">{{ c.title ?? "-" }} · {{ c.email }} <span v-if="c.phone">· {{ c.phone }}</span></div>
          </li>
        </ul>
        <EmptyState v-else class="mt-3">Henüz yetkili kişi yok</EmptyState>
      </AppCard>

      <AppCard>
        <div class="flex items-center justify-between">
          <h2 class="text-sm font-semibold text-slate-700">Adresler</h2>
          <button class="text-xs font-medium text-indigo-600 hover:underline" @click="showAddressForm = !showAddressForm">
            {{ showAddressForm ? "Vazgeç" : "+ Yeni" }}
          </button>
        </div>

        <form v-if="showAddressForm" class="mt-3 grid grid-cols-2 gap-2" @submit.prevent="addAddress">
          <select v-model="addressForm.addressType" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm">
            <option v-for="t in addressTypes" :key="t" :value="t">{{ t }}</option>
          </select>
          <input v-model="addressForm.country" placeholder="Ülke" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="addressForm.city" placeholder="Şehir" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="addressForm.district" placeholder="İlçe (opsiyonel)" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="addressForm.postalCode" placeholder="Posta Kodu (opsiyonel)" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="addressForm.line1" placeholder="Adres Satırı 1" required class="col-span-2 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="addressForm.line2" placeholder="Adres Satırı 2 (opsiyonel)" class="col-span-2 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <button type="submit" class="col-span-2 rounded-md bg-slate-800 px-3 py-1.5 text-sm font-semibold text-white">Kaydet</button>
        </form>

        <ul v-if="company.addresses.length > 0" class="mt-3 space-y-2 text-sm">
          <li v-for="a in company.addresses" :key="a.id" class="rounded-lg border border-slate-100 p-2">
            <div class="font-medium text-slate-700">{{ a.addressType }}</div>
            <div class="text-xs text-slate-500">
              {{ a.line1 }}<span v-if="a.line2">, {{ a.line2 }}</span>, {{ a.district ? a.district + " / " : "" }}{{ a.city }}
              <span v-if="a.postalCode">({{ a.postalCode }})</span>, {{ a.country }}
            </div>
          </li>
        </ul>
        <EmptyState v-else class="mt-3">Henüz adres yok</EmptyState>
      </AppCard>
    </div>
  </div>
</template>
