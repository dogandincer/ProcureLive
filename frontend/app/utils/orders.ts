/** Sipariş durumu → rozet tonu (liste ve detay ekranları ortak). */
export const orderStatusTone: Record<string, "slate" | "amber" | "emerald" | "indigo" | "rose"> = {
  Draft: "slate",
  Sent: "amber",
  Confirmed: "emerald",
  Cancelled: "rose",
};

/** Siparişin kaynağı olan sürecin ekran yolu. */
export function orderSourcePath(kind: string, id: string, internal: boolean): string {
  const base = internal ? "/admin" : "/supplier";
  if (kind === "Rfq") return `${base}/rfq/${id}`;
  if (kind === "Rfi") return `${base}/rfi/${id}`;
  return `${base}/auctions/${id}`;
}
