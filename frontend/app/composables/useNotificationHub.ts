import * as signalR from "@microsoft/signalr";
import type { NotificationDto } from "~/types/api";

let sharedConnection: signalR.HubConnection | null = null;
let connectPromise: Promise<signalR.HubConnection> | null = null;

/**
 * Single shared connection to /hubs/notifications for the whole app (the layout's bell icon owns
 * its lifecycle). The server auto-joins the caller to their own company/internal group on connect
 * (NotificationHub.OnConnectedAsync) — the client never chooses which group to join.
 */
export function useNotificationHub() {
  const config = useRuntimeConfig();
  const { accessToken } = useAuth();

  async function connect(onNotification: (notification: NotificationDto) => void): Promise<void> {
    if (!connectPromise) {
      sharedConnection = new signalR.HubConnectionBuilder()
        .withUrl(`${config.public.apiBaseUrl}/hubs/notifications`, {
          accessTokenFactory: () => accessToken.value ?? "",
        })
        .withAutomaticReconnect()
        .build();

      connectPromise = sharedConnection.start().then(() => sharedConnection!);
    }

    const conn = await connectPromise;
    conn.on("NotificationReceived", onNotification);
  }

  return { connect };
}
