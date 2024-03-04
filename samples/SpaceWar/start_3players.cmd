dotnet build -c Release
start dotnet run --no-build -c Release -- 9000 3 local 127.0.0.1:9001 127.0.0.1:9002
start dotnet run --no-build -c Release -- 9001 3 127.0.0.1:9000 local 127.0.0.1:9002
start dotnet run --no-build -c Release -- 9002 3 127.0.0.1:9000 127.0.0.1:9001 local 
