dotnet build
start dotnet run --no-build -- 9000 local 127.0.0.1:9001 
start dotnet run --no-build -- 9001 127.0.0.1:9000 local
