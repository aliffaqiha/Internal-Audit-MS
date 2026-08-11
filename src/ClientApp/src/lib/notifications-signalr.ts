import { HubConnectionBuilder, HubConnectionState, type HubConnection } from "@microsoft/signalr"

import { tokenStore } from "./api"

let connection: HubConnection | null = null

export function connectNotificationsHub(): HubConnection {
  if (connection && connection.state !== HubConnectionState.Disconnected) {
    return connection
  }

  connection = new HubConnectionBuilder()
    .withUrl("/hubs/notifications", {
      accessTokenFactory: () => tokenStore.accessToken ?? "",
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
}