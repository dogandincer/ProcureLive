import * as signalR from "@microsoft/signalr";

/**
 * One live connection to /hubs/auction, joined to a single auction's group. JWT is passed via the
 * access_token query string (WebSocket connections cannot carry an Authorization header) — the
 * backend explicitly accepts that for /hubs/* paths (see Program.cs JwtBearerEvents.OnMessageReceived).
 */
export function useAuctionHub() {
  const config = useRuntimeConfig();
  const { accessToken } = useAuth();

  let connection: signalR.HubConnection | null = null;
  let joinedAuctionId: string | null = null;

  async function connectAndJoin(auctionId: string): Promise<signalR.HubConnection> {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(`${config.public.apiBaseUrl}/hubs/auction`, {
        accessTokenFactory: () => accessToken.value ?? "",
      })
      .withAutomaticReconnect()
      .build();

    await connection.start();
    await connection.invoke("JoinAuctionGroup", auctionId);
    joinedAuctionId = auctionId;

    connection.onreconnected(async () => {
      if (joinedAuctionId) {
        await connection?.invoke("JoinAuctionGroup", joinedAuctionId);
      }
    });

    return connection;
  }

  async function disconnect(): Promise<void> {
    if (connection && joinedAuctionId) {
      try {
        await connection.invoke("LeaveAuctionGroup", joinedAuctionId);
      } catch {
        // connection may already be closed; nothing to clean up on the server side then.
      }
    }

    await connection?.stop();
    connection = null;
    joinedAuctionId = null;
  }

  return { connectAndJoin, disconnect };
}
