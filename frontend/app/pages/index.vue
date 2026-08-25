<script setup lang="ts">
definePageMeta({ layout: false });

const { homePathForRole } = useAuth();

onMounted(() => {
  navigateTo(homePathForRole());
});
</script>

<template>
  <div />
</template>
