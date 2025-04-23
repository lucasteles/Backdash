dotnet build -c Release %~dp0\..\..
pushd %~dp0\..\..\bin\Release\net8.0
del *.log
start SpaceWar 0 1 sync-test-auto
popd
