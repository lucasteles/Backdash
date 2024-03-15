#!/bin/bash
pushd "$(dirname "$0")/../../../LobbyServer" || exit
dotnet run --no-build -c Release
popd || exit
