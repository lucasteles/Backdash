dotnet build -c Release %~dp0\..\..\..
pushd %~dp0\..\..\bin\Release\net8.0
del *.log
@set LOBBY_SERVER_URL=https://lobby-server.fly.dev

start SpaceWar -ServerURl %LOBBY_SERVER_URL% -LocalPort 9000 -Username ryu
start SpaceWar -ServerURl %LOBBY_SERVER_URL% -LocalPort 9001 -Username liu_kang
start SpaceWar -ServerURl %LOBBY_SERVER_URL% -LocalPort 9002 -Username kyo
start SpaceWar -ServerURl %LOBBY_SERVER_URL% -LocalPort 9003 -Username jin
start SpaceWar -ServerURl %LOBBY_SERVER_URL% -LocalPort 9004 -Username sol_badguy
start SpaceWar -ServerURl %LOBBY_SERVER_URL% -LocalPort 9005 -Username jago

popd
