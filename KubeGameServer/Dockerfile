FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY KubeGameServer/KubeGameServer.csproj KubeGameServer/
RUN dotnet restore KubeGameServer/KubeGameServer.csproj
COPY KubeGameServer/ KubeGameServer/
RUN dotnet publish KubeGameServer/KubeGameServer.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
EXPOSE 7777
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KubeGameServer.dll"]
