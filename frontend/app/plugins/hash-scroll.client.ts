/**
 * Bildirimden "#qa", "#documents" gibi bir bölüme gidilince: bölüm veri yüklendikten sonra oluştuğu için
 * öğe görünene kadar kısa süre bekleyip oraya kaydırır (en fazla ~4 sn).
 */
export default defineNuxtPlugin(() => {
  const router = useRouter();
  router.afterEach((to) => {
    if (!to.hash) return;
    const id = decodeURIComponent(to.hash.slice(1));
    let tries = 0;
    const timer = setInterval(() => {
      const el = document.getElementById(id);
      if (el) el.scrollIntoView({ behavior: "smooth", block: "start" });
      if (el || ++tries > 20) clearInterval(timer);
    }, 200);
  });
});
