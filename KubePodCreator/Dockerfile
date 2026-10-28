FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY KubePodCreator/KubePodCreator.csproj KubePodCreator/
RUN dotnet restore KubePodCreator/KubePodCreator.csproj
COPY KubePodCreator/ KubePodCreator/
RUN dotnet publish KubePodCreator/KubePodCreator.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KubePodCreator.dll"]
