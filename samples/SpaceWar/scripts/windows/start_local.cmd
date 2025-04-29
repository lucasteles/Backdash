dotnet build -c Release %~dp0\..\..
pushd %~dp0\..\..\bin\Release\net8.0
del *.log
del logs\*.log
start SpaceWar 0 2 local-only
popd
