dotnet build -c Release %~dp0\..\..\..\..
pushd %~dp0\..\..\..\LobbyServer

@set SPACEWAR_LOBBY_URL=http://localhost:9999
start dotnet run --no-build -c Release
popd
