#!/bin/bash
dotnet build -c Release "$(dirname "$0")/../../../.."
export LOBBY_SERVER_URL=http://localhost:9999
