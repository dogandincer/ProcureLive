// Registers the <model-viewer> custom element for interactive 3D product viewing.
// Client-only: the library touches window/customElements, which don't exist during build.
export default defineNuxtPlugin(async () => {
  await import("@google/model-viewer");
});
