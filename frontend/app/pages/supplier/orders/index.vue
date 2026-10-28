<script setup lang="ts">
/** Tedarikçiye gönderilen siparişler (taslaklar görünmez). */
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-xl font-bold text-slate-800">{{ $t("nav.myOrders") }}</h1>
      <p class="mt-1 text-sm text-slate-500">{{ $t("orders.supplierIntro") }}</p>
    </div>
    <AppCard>
      <PurchaseOrderList :internal="false" />
    </AppCard>
  </div>
</template>
