#!/bin/bash
dotnet build -c Release "$(dirname "$0")/../.."
pushd "$(dirname "$0")/../../bin/Release/net8.0" || exit
rm ./*.log
rm ./logs/*.log
dotnet SpaceWar.dll 9000 2 local 127.0.0.1:9001 s:127.0.0.1:9100 &
dotnet SpaceWar.dll 9001 2 127.0.0.1:9000 local s:127.0.0.1:9101 &
dotnet SpaceWar.dll 9100 2 spectate 127.0.0.1:9000 &
dotnet SpaceWar.dll 9101 2 spectate 127.0.0.1:9001 &
popd || exit
