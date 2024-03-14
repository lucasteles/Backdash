FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
# build application
COPY ./ /build
WORKDIR /build
RUN dotnet publish \
    --configuration Release \
    --output /app \
    --runtime linux-musl-x64 \
    /p:PublishSingleFile=true

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine-amd64 as final
EXPOSE 8080
EXPOSE 8888
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["./LobbyServer"]
