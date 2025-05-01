del logs\*.log
dotnet build -c Debug
start dotnet run --no-build -- 9000 2 play local 127.0.0.1:9001 s:127.0.0.1:9100
start dotnet run --no-build -- 9001 2 play 127.0.0.1:9000 local s:127.0.0.1:9101
start dotnet run --no-build -- 9100 2 spectate 127.0.0.1:9000
start dotnet run --no-build -- 9101 2 spectate 127.0.0.1:9001
