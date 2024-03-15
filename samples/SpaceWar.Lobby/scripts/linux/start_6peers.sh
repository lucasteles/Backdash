#!/bin/bash
source "$(dirname "$0")/build_server.sh"
pushd "$(dirname "$0")/../../bin/Release/net8.0" || exit
rm ./*.log

dotnet SpaceWar.dll 9000 &
dotnet SpaceWar.dll 9001 &
dotnet SpaceWar.dll 9002 &
dotnet SpaceWar.dll 9003 &
dotnet SpaceWar.dll 9004 &
dotnet SpaceWar.dll 9005 &

popd || exit
source "$(dirname "$0")/start_server.sh"
