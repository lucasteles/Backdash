dotnet build -c Release %~dp0\..\..
pushd %~dp0\..\..\bin\Release\net8.0
del *.log

start SpaceWar 9000 2 local 127.0.0.1:9001 127.0.0.1:9100
start SpaceWar 9001 2 127.0.0.1:9000 local 127.0.0.1:9101

start SpaceWar 9100 2 spectate 127.0.0.1:9000
start SpaceWar 9101 2 spectate 127.0.0.1:9001

popd
