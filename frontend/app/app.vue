<script setup lang="ts">
// Sekme başlığı ve favicon varsayılan firmanın markasından gelir (Firma Tanım ekranında değiştirilebilir).
const { branding, logoSrc, load } = useBranding();

useHead(() => ({
  title: branding.value.appName,
  link: logoSrc.value ? [{ rel: "icon", href: logoSrc.value, key: "favicon" }] : [],
}));

onMounted(() => load());
</script>

<template>
  <NuxtLayout>
    <NuxtPage />
  </NuxtLayout>
  <AppToastContainer />
</template>
