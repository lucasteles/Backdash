dotnet build -c Release %~dp0\..\..
pushd %~dp0\..\..\bin\Release\net8.0
del *.log
start SpaceWar 9000 2 replay replay.inputs
popd
