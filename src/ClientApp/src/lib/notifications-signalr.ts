import { HubConnectionBuilder, HubConnectionState, type HubConnection } from "@microsoft/signalr"

import { api } from "./api"

let connection: HubConnection | null = null

let hubToken: { token: string; expiresAt: number } | null = null

/**
 * Fetches a short-lived SignalR hub token from the API and caches it until it
 * is close to expiry. The hub token expires in ~2 minutes and carries no roles,
 * so the token exposed via the WebSocket query string is of limited value.
 */
async function getHubToken(): Promise<string> {
  const now = Date.now()
  if (hubToken && hubToken.expiresAt > now + 30_000) {
    return hubToken.token
  }

  const { data } = await api.post<{ accessToken: string; accessTokenExpiresAt: string }>(
    "/auth/signalr-token"
  )
  hubToken = {
    token: data.accessToken,
    expiresAt: Date.parse(data.accessTokenExpiresAt),
  }
  return hubToken.token
}

export function connectNotificationsHub(): HubConnection {
  if (connection && connection.state !== HubConnectionState.Disconnected) {
    return connection
  }

  connection = new HubConnectionBuilder()
    .withUrl("/hubs/notifications", {
      accessTokenFactory: () => getHubToken(),
    })
    .withAutomaticReconnect()
    .build()

  void connection.start().catch(() => undefined)

  return connection
}

export function disconnectNotificationsHub(): void {
  if (!connection) return
  void connection.stop()
  connection = null
  hubToken = null
}
