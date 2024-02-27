dotnet build -c Release

start dotnet run --no-build -c Release -- 9000 2 local 127.0.0.1:9001 127.0.0.1:9100
start dotnet run --no-build -c Release -- 9001 2 127.0.0.1:9000 local 127.0.0.1:9101

start dotnet run --no-build -c Release -- 9100 2 spectate 127.0.0.1:9000
start dotnet run --no-build -c Release -- 9101 2 spectate 127.0.0.1:9001
