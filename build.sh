#!/usr/bin/env bash

SCRIPT_DIR=$(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)
BUILD_PROJECT_FILE="$SCRIPT_DIR/build/_build.csproj"
dotnet tool restore >/dev/null
dotnet build "$BUILD_PROJECT_FILE" /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet
dotnet run --project "$BUILD_PROJECT_FILE" --no-build -- "$@"
