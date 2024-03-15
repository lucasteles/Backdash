#!/bin/bash

publish() {
    output_path="./Artifacts/spacewar_$1"
    dotnet publish . \
        --configuration Release --self-contained \
        --output "$output_path" -p:PublishSingleFile=true \
        -r $1 -p:DebugType=None -p:DebugSymbols=false

    if [[ "$1" == *"win"* ]]; then
      find "$output_path" -name "*.sh" -delete
    else
      find "$output_path" -name "*.cmd" -delete
    fi
}

publish "win-x64"
publish "linux-x64"
publish "osx-x64"
publish "osx-arm64"
