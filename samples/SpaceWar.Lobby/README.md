# SpaceWar - NetCode Sample with online Lobby

This shows a basic example of NAT traversal using [UDP hole punching](https://en.wikipedia.org/wiki/UDP_hole_punching)

# How it works?

This enable a P2P connection over the internet, this is possible using
a [middle server](https://github.com/lucasteles/Backdash/tree/master/samples/LobbyServer)
which all clients know.
The server catches the IP address and port of a client and send it to the others.

The current server runs almost as a simple http with json responses. It keeps the lobbies info with sliding expiration
cache.

When a client enters the lobby the server responds with a token of type `Guid`/`UUID`. It is used a very
basic `Authentication` mechanism.

The client use http pooling to get updated information of each lobby member/peer.

When logged-in every client needs to send a `UDP` package with their token to the server. So the
server updates their `IP` and open `Port` using the package headers metadata.

> ⚠️ UDP Hole punching usually **does not** work witch clients behind the same NAT. To mitigate this the server
> also tracks the clients local IPs and ports. So they can check if the peer is at the same network

## Controls

- **Arrows**: Move
- **Left Control**: Fire
- **Enter**: Missile

## Running

### Server

On the [server directory](https://github.com/lucasteles/Backdash/tree/master/samples/LobbyServer) run:

```bash
dotnet run .
```

- Default **HTTP**: `9999`
- Default **UDP** : `8888`

> [!TIP]
> 💡 Check the swagger `API` docs at http://localhost:9999/swagger

### Clients

On `SpaceWar.Lobby` project directory run

```bash
dotnet run .
```

The default client configuration is defined in this [JSON file](/appsettings.json):

```json
{
    "LobbyName": "spacewar",
    "LocalPort": 9000,
    "ServerUrl": "http://localhost:9999",
    "ServerUdpPort": 8888
}
```

You can override the default port via command args:

```sh
dotnet run --project .\LobbyClient -LocalPort 9001
```

> [!TIP]
> 💡useful for starting clients in different ports


You can also override the server URL and UDP Port configs:

```bash
dotnet run --project .\LobbyClient -ServerUrl "https://lobby-server.fly.dev" -ServerUdpPort 8888
```

Check the the [scripts directory](https://github.com/lucasteles/Backdash/tree/master/samples/SpaceWar.Lobby/scripts)
to run local instances of the server and clients.

> ⚠️ The default configured server is a remote server. To connect to localhost server
> run: `dotnet run . -ServerUrl "http://localhost:9999"`

