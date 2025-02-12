#!/bin/bash
dotnet build -c Release "$(dirname "$0")/../../.."
pushd "$(dirname "$0")/../../../LobbyServer" || exit
dotnet run --no-build
popd || exit
