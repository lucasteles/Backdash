#!/bin/bash
dotnet build -c Release "$(dirname "$0")/../.."
pushd "$(dirname "$0")/../../bin/Release/net8.0" || exit
rm ./*.log

dotnet SpaceWar.dll 9000 3 local 127.0.0.1:9001 127.0.0.1:9002 &
dotnet SpaceWar.dll 9001 3 127.0.0.1:9000 local 127.0.0.1:9002 &
dotnet SpaceWar.dll 9002 3 127.0.0.1:9000 127.0.0.1:9001 local &

popd || exit
