dotnet build -c Release %~dp0\..\..
pushd %~dp0\..\..\bin\Release\net8.0
del *.log
start SpaceWar 9000 user1
start SpaceWar 9001 user2
popd
