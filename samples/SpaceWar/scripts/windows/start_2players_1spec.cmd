dotnet build -c Release %~dp0\..\..
pushd %~dp0\..\..\bin\Release\net8.0
del *.log
del logs\*.log
start SpaceWar 9000 2 local 127.0.0.1:9001 s:127.0.0.1:9100
start SpaceWar 9001 2 127.0.0.1:9000 local
start SpaceWar 9100 2 spectate 127.0.0.1:9000
popd
