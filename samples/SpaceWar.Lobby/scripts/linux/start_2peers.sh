#!/bin/bash
dotnet build -c Release "$(dirname "$0")/../.."
pushd "$(dirname "$0")/../../bin/Release/net8.0" || exit
rm ./*.log
dotnet SpaceWar.dll 9000 user1
dotnet SpaceWar.dll 9001 user2
popd || exit
