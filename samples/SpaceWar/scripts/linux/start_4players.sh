#!/bin/bash
dotnet build -c Release "$(dirname "$0")/../.."
pushd "$(dirname "$0")/../../bin/Release/net8.0" || exit
rm ./*.log
rm ./logs/*.log
dotnet SpaceWar.dll 9000 4 local 127.0.0.1:9001 127.0.0.1:9002 127.0.0.1:9003 &
dotnet SpaceWar.dll 9001 4 127.0.0.1:9000 local 127.0.0.1:9002 127.0.0.1:9003 &
dotnet SpaceWar.dll 9002 4 127.0.0.1:9000 127.0.0.1:9001 local 127.0.0.1:9003 &
dotnet SpaceWar.dll 9003 4 127.0.0.1:9000 127.0.0.1:9001 127.0.0.1:9002 local &
popd || exit
