#!/bin/bash
pushd "$(dirname "$0")/../../../LobbyServer" || exit
dotnet run -c Release
popd || exit
