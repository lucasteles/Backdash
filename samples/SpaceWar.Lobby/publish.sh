#!/bin/bash

publish() {
    output_path="./artifacts/spacewar_$1"
    dotnet publish . \
        --configuration Release --self-contained \
        --output "$output_path" \
        -r $1 -p:DebugType=None -p:DebugSymbols=false
}

publish "win-x64"
publish "linux-x64"
publish "osx-x64"
publish "osx-arm64"
