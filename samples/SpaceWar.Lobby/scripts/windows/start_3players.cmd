dotnet build -c Release %~dp0\..\..
pushd %~dp0\..\..\bin\Release\net8.0
del *.log
start SpaceWar 9000 3 local 127.0.0.1:9001 127.0.0.1:9002
start SpaceWar 9001 3 127.0.0.1:9000 local 127.0.0.1:9002
start SpaceWar 9002 3 127.0.0.1:9000 127.0.0.1:9001 local 
popd
