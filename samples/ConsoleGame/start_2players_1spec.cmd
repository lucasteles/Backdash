dotnet build -c Debug
start dotnet run --no-build -- 9000 2 local 127.0.0.1:9001 s:127.0.0.1:9100
start dotnet run --no-build -- 9001 2 127.0.0.1:9000 local
start dotnet run --no-build -- 9100 2 spectate 127.0.0.1:9000
