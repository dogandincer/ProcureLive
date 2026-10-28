import type { UploadedFileDto } from "~/types/api";

export type UploadPurpose = "auction-attachment" | "rfi-answer" | "product-image" | "company-document";

/** Sunucuya gerçek dosya yükleme (POST /api/files). Tür, boyut ve içerik imzası sunucuda doğrulanır. */
export function useFileUpload() {
  const api = useApi();

  async function upload(file: File, purpose: UploadPurpose): Promise<UploadedFileDto> {
    const data = new FormData();
    data.append("file", file);
    return api.post<UploadedFileDto>(`/api/files?purpose=${purpose}`, data);
  }

  return { upload };
}
