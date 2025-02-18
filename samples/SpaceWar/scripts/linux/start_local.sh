#!/bin/bash
dotnet build -c Release "$(dirname "$0")/../.."
pushd "$(dirname "$0")/../../bin/Release/net8.0" || exit
rm ./*.log
dotnet SpaceWar.dll 0 2 local-only
popd || exit
