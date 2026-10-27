import type { NotificationDto } from "~/types/api";

/** Son gelen canlı bildirim — sayfalar ilgili kayıt için gelen bildirimi izleyip kendini yenileyebilir. */
export const useLastNotification = () => useState<NotificationDto | null>("pl_last_notification", () => null);
