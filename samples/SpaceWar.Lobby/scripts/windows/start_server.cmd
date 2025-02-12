dotnet build -c Release %~dp0\..\..\..
@pushd %~dp0\..\..\..\LobbyServer

@set LOBBY_SERVER_URL=http://localhost:9999
start dotnet run --no-build
@popd
