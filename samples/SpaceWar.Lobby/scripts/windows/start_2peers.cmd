call %~dp0\start_server.cmd
@pushd %~dp0\..\..\bin\Release\net8.0
del *.log
start SpaceWar 9000
start SpaceWar 9001
@popd
