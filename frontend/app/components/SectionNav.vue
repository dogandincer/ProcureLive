<script setup lang="ts">
/**
 * Uzun detay sayfaları için yapışkan bölüm gezintisi: başlıklara atlar, görünür bölümü işaretler.
 * Bölümler sayfada `id` taşıyan öğelerdir; görünmeyen (v-if ile yok) bölüm otomatik gizlenir.
 */
const props = defineProps<{ sections: { id: string; label: string; count?: number | null }[] }>();

const active = ref<string | null>(null);
const present = ref<Set<string>>(new Set());
let observer: IntersectionObserver | null = null;
let poll: ReturnType<typeof setInterval> | null = null;

const visible = computed(() => props.sections.filter((s) => present.value.has(s.id)));

function scan() {
  const found = new Set(props.sections.map((s) => s.id).filter((id) => document.getElementById(id)));
  if ([...found].join() !== [...present.value].join()) {
    present.value = found;
    observe();
  }
}

function observe() {
  observer?.disconnect();
  observer = new IntersectionObserver(
    (entries) => {
      const top = entries.filter((e) => e.isIntersecting).sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top)[0];
      if (top) active.value = top.target.id;
    },
    { rootMargin: "-80px 0px -60% 0px" },
  );
  for (const id of present.value) {
    const el = document.getElementById(id);
    if (el) observer.observe(el);
  }
}

function go(id: string) {
  const el = document.getElementById(id);
  if (!el) return;
  active.value = id;
  el.scrollIntoView({ behavior: "smooth", block: "start" });
  history.replaceState(history.state, "", `#${id}`);
}

onMounted(() => {
  scan();
  poll = setInterval(scan, 1000); // bölümler veri yüklendikçe ortaya çıkar
});
onBeforeUnmount(() => {
  observer?.disconnect();
  if (poll) clearInterval(poll);
});
</script>

<template>
  <nav
    v-if="visible.length > 1"
    :aria-label="$t('sectionNav.label')"
    class="sticky top-14 z-20 -mx-4 overflow-x-auto border-b border-slate-200 bg-slate-100/95 px-4 py-2 backdrop-blur sm:-mx-6 sm:px-6 print:hidden"
    data-testid="section-nav"
  >
    <ul class="flex min-w-max gap-1">
      <li v-for="s in visible" :key="s.id">
        <button
          type="button"
          class="inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-medium transition-colors"
          :class="active === s.id ? 'bg-indigo-600 text-white' : 'text-slate-600 hover:bg-slate-200/70'"
          :aria-current="active === s.id ? 'true' : undefined"
          @click="go(s.id)"
        >
          {{ s.label }}
          <span v-if="s.count" class="rounded-full px-1.5 text-[10px]" :class="active === s.id ? 'bg-white/25' : 'bg-slate-200 text-slate-600'">{{ s.count }}</span>
        </button>
      </li>
    </ul>
  </nav>
</template>
