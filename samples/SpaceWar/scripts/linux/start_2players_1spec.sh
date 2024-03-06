#!/bin/bash
dotnet build -c Release "$(dirname "$0")/../.."
pushd "$(dirname "$0")/../../bin/Release/net8.0" || exit
rm ./*.log

dotnet SpaceWar.dll 9000 2 local 127.0.0.1:9001 127.0.0.1:9100 &
dotnet SpaceWar.dll 9001 2 127.0.0.1:9000 local &
dotnet SpaceWar.dll 9100 2 spectate 127.0.0.1:9000 &

popd || exit
