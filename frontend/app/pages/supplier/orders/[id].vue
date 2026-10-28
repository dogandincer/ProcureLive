<script setup lang="ts">
const route = useRoute();
</script>

<template>
  <PurchaseOrderDetail :order-id="(route.params.id as string)" :internal="false" />
</template>
