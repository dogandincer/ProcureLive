/**
 * Uygulama içi onay penceresi (tarayıcının confirm() kutusu yerine). `ask` bir söz döndürür:
 * kullanıcı onaylarsa true, vazgeçerse (Esc, arka plan, Vazgeç) false. Pencereyi ConfirmHost (app.vue) çizer.
 */
export interface ConfirmOptions {
  title: string;
  body?: string;
  confirmLabel?: string;
  tone?: "primary" | "danger";
}

let resolver: ((value: boolean) => void) | null = null;

export function useConfirm() {
  const state = useState<(ConfirmOptions & { open: boolean }) | null>("confirmDialog", () => null);

  function ask(options: ConfirmOptions): Promise<boolean> {
    resolver?.(false);
    state.value = { ...options, open: true };
    return new Promise((resolve) => {
      resolver = resolve;
    });
  }

  function settle(value: boolean) {
    state.value = null;
    const r = resolver;
    resolver = null;
    r?.(value);
  }

  return { state, ask, settle };
}
