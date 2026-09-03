<script setup lang="ts">
import type { CompanyDetailDto } from "~/types/api";

const route = useRoute();
const companyId = route.params.id as string;
const api = useApi();

const company = ref<CompanyDetailDto | null>(null);
const errorMessage = ref("");
const showContactForm = ref(false);
const showAddressForm = ref(false);

const contactForm = reactive({ firstName: "", lastName: "", title: "", email: "", phone: "", isPrimary: false });
const addressTypes = ["HeadOffice", "Billing", "Shipping"];
const addressForm = reactive({ addressType: "HeadOffice", country: "Türkiye", city: "", district: "", postalCode: "", line1: "", line2: "" });

async function load() {
  company.value = await api.get<CompanyDetailDto>(`/api/companies/${companyId}`);
}

async function addContact() {
  errorMessage.value = "";
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
    await load();
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, "Yetkili kişi eklenemedi.");
  }
}

async function addAddress() {
  errorMessage.value = "";
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
    await load();
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, "Adres eklenemedi.");
  }
}

onMounted(load);
</script>

<template>
  <div v-if="company" class="space-y-6">
    <div>
      <NuxtLink to="/admin/companies" class="text-xs text-indigo-600 hover:underline">← Cariler</NuxtLink>
      <h1 class="mt-1 text-xl font-bold text-slate-800">{{ company.name }}</h1>
      <p class="text-sm text-slate-500">{{ company.taxNumber }} · {{ company.taxOffice }} · {{ company.companyType }} · {{ company.status }}</p>
    </div>

    <div v-if="errorMessage" class="flex items-center justify-between rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">
      <span>{{ errorMessage }}</span>
      <button class="font-semibold" @click="errorMessage = ''">Kapat</button>
    </div>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <div class="rounded-xl border border-slate-200 bg-white p-5">
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

        <ul class="mt-3 space-y-2 text-sm">
          <li v-for="c in company.contactPersons" :key="c.id" class="rounded-lg border border-slate-100 p-2">
            <div class="flex items-center justify-between">
              <span class="font-medium text-slate-700">{{ c.firstName }} {{ c.lastName }}</span>
              <span v-if="c.isPrimary" class="rounded bg-indigo-100 px-2 py-0.5 text-xs font-medium text-indigo-700">Birincil</span>
            </div>
            <div class="text-xs text-slate-500">{{ c.title ?? "-" }} · {{ c.email }} <span v-if="c.phone">· {{ c.phone }}</span></div>
          </li>
          <li v-if="company.contactPersons.length === 0" class="text-slate-400">Henüz yetkili kişi yok</li>
        </ul>
      </div>

      <div class="rounded-xl border border-slate-200 bg-white p-5">
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

        <ul class="mt-3 space-y-2 text-sm">
          <li v-for="a in company.addresses" :key="a.id" class="rounded-lg border border-slate-100 p-2">
            <div class="font-medium text-slate-700">{{ a.addressType }}</div>
            <div class="text-xs text-slate-500">
              {{ a.line1 }}<span v-if="a.line2">, {{ a.line2 }}</span>, {{ a.district ? a.district + " / " : "" }}{{ a.city }}
              <span v-if="a.postalCode">({{ a.postalCode }})</span>, {{ a.country }}
            </div>
          </li>
          <li v-if="company.addresses.length === 0" class="text-slate-400">Henüz adres yok</li>
        </ul>
      </div>
    </div>
  </div>
</template>
