export interface ToastMessage {
  id: number;
  type: "success" | "error" | "info";
  text: string;
}

const toasts = ref<ToastMessage[]>([]);
let nextId = 1;

const DURATION_MS: Record<ToastMessage["type"], number> = {
  success: 4000,
  info: 4000,
  error: 6000,
};

function push(type: ToastMessage["type"], text: string) {
  const id = nextId++;
  toasts.value.push({ id, type, text });
  setTimeout(() => dismiss(id), DURATION_MS[type]);
}

function dismiss(id: number) {
  toasts.value = toasts.value.filter((t) => t.id !== id);
}

/**
 * Global toast queue - a single shared `toasts` ref (module scope, not per-call state) so every
 * component that calls useToast() reads/writes the same list. Mounted once via AppToastContainer
 * in app.vue, so it works on every page regardless of layout (including login.vue's layout:false).
 */
export function useToast() {
  return {
    toasts,
    success: (text: string) => push("success", text),
    error: (text: string) => push("error", text),
    info: (text: string) => push("info", text),
    dismiss,
  };
}
