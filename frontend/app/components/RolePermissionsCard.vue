<script setup lang="ts">
import type { RolePermissionDto } from "~/types/api";

/**
 * Satınalmacı rolünün işlem yetkileri (yalnızca Admin). Kaldırılan yetki tüm satınalmacılarda hem düğmeyi gizler hem de
 * sunucuda işlemi reddeder (403). Firma/işyeri kapsamı bundan bağımsızdır (aşağıdaki işyeri yetkisi).
 */
const api = useApi();
const toast = useToast();
const { t } = useI18n();

const rows = ref<RolePermissionDto[]>([]);
const selected = ref<Set<string>>(new Set());
const original = ref<Set<string>>(new Set());
const loading = ref(true);
const saving = ref(false);

const dirty = computed(() => selected.value.size !== original.value.size || [...selected.value].some((c) => !original.value.has(c)));

function toggle(code: string) {
  const next = new Set(selected.value);
  if (next.has(code)) next.delete(code);
  else next.add(code);
  selected.value = next;
}

async function load() {
  try {
    rows.value = await api.get<RolePermissionDto[]>("/api/permissions/buyer");
    selected.value = new Set(rows.value.filter((r) => r.granted).map((r) => r.code));
    original.value = new Set(selected.value);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("permissions.loadError")));
  } finally {
    loading.value = false;
  }
}

async function save() {
  saving.value = true;
  try {
    await api.put("/api/permissions/buyer", { codes: [...selected.value] });
    original.value = new Set(selected.value);
    toast.success(t("permissions.role.saved"));
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("permissions.saveError")));
  } finally {
    saving.value = false;
  }
}

onMounted(load);
</script>

<template>
  <AppCard :title="$t('permissions.role.title')" :description="$t('permissions.role.hint')">
    <LoadingSpinner v-if="loading" />
    <div v-else class="space-y-4" data-testid="role-permissions">
      <ul class="grid grid-cols-1 gap-2 sm:grid-cols-2">
        <li v-for="r in rows" :key="r.code">
          <label class="flex cursor-pointer items-start gap-3 rounded-lg border px-3 py-2 text-sm" :class="selected.has(r.code) ? 'border-indigo-200 bg-indigo-50/50' : 'border-slate-200'">
            <input type="checkbox" class="mt-0.5 rounded border-slate-300" :checked="selected.has(r.code)" :data-testid="`perm-${r.code}`" @change="toggle(r.code)" />
            <span class="font-medium text-slate-800">{{ $t(`permissions.codes.${r.code.replace('.', '_')}`) }}</span>
          </label>
        </li>
      </ul>
      <div class="flex items-center gap-3">
        <AppButton :loading="saving" :disabled="!dirty" data-testid="role-permissions-save" @click="save">{{ $t("common.save") }}</AppButton>
        <span v-if="dirty" class="text-xs text-amber-600">{{ $t("permissions.unsaved") }}</span>
      </div>
    </div>
  </AppCard>
</template>
