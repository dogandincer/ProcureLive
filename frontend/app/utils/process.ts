/** RFI/RFQ süreç durumu → rozet tonu (liste ve detay ekranları ortak). */
export const processStatusTone: Record<string, "slate" | "amber" | "emerald" | "indigo" | "rose"> = {
  Draft: "slate",
  Sent: "amber",
  InProgress: "amber",
  Closed: "slate",
  Evaluated: "indigo",
  Awarded: "indigo",
  Cancelled: "rose",
};
