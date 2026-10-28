<script setup lang="ts">
/** İç kullanıcı sipariş listesi (başlıktaki firma/işyeri seçimine göre). Sipariş, sonuçlanan ihale veya RFQ ekranından açılır. */
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-xl font-bold text-slate-800">{{ $t("orders.title") }}</h1>
      <p class="mt-1 text-sm text-slate-500">{{ $t("orders.intro") }}</p>
    </div>
    <AppCard>
      <PurchaseOrderList internal />
    </AppCard>
  </div>
</template>
