#!/bin/bash
dotnet build -c Release "$(dirname "$0")/../../../.."
pushd "$(dirname "$0")/../../bin/Release/net8.0" || exit
rm ./*.log

export LOBBY_SERVER_URL=https://lobby-server.fly.dev

dotnet SpaceWar.dll -ServerURl "$LOBBY_SERVER_URL" -LocalPort 9000 -Username ryu  &
dotnet SpaceWar.dll -ServerURl "$LOBBY_SERVER_URL" -LocalPort 9001 -Username liu_kang &
dotnet SpaceWar.dll -ServerURl "$LOBBY_SERVER_URL" -LocalPort 9002 -Username kyo &
dotnet SpaceWar.dll -ServerURl "$LOBBY_SERVER_URL" -LocalPort 9003 -Username jin &
dotnet SpaceWar.dll -ServerURl "$LOBBY_SERVER_URL" -LocalPort 9004 -Username sol_badguy &
dotnet SpaceWar.dll -ServerURl "$LOBBY_SERVER_URL" -LocalPort 9005 -Username jago &

popd || exit
source "$(dirname "$0")/start_server.sh"
