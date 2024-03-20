# Builds the Backdash API documentation using docfx

$prevPwd = $PWD; Set-Location -ErrorAction Stop -LiteralPath $PSScriptRoot

try {
    $PWD  # output the current location

    dotnet tool restore
    dotnet build -c Release ../Backdash.sln

    # Force delete metadata
    rm ./api  -Recurse -Force -ErrorAction SilentlyContinue
    rm ./_site  -Recurse -Force -ErrorAction SilentlyContinue

    $env:DOCFX_SOURCE_BRANCH_NAME="master"

    dotnet docfx --serve
}
finally {
  # Restore the previous location.
  $prevPwd | Set-Location
}
