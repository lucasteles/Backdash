#!/bin/bash
dotnet build -c Release "$(dirname "$0")/../../../.."
export SPACEWAR_LOBBY_URL=http://localhost:9999
