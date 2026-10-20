FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG PP_PROVIDER=rosu
WORKDIR /src
COPY ["BanchoNET/BanchoNET.csproj", "BanchoNET/"]
COPY ["BanchoNET.BeatmapServer/BanchoNET.BeatmapServer.csproj", "BanchoNET.BeatmapServer/"]
COPY ["BanchoNET.Bot/BanchoNET.Bot.csproj", "BanchoNET.Bot/"]
COPY ["BanchoNET.Core/BanchoNET.Core.csproj", "BanchoNET.Core/"]
COPY ["BanchoNET.Handlers.Lazer/BanchoNET.Handlers.Lazer.csproj", "BanchoNET.Handlers.Lazer/"]
COPY ["BanchoNET.Infrastructure/BanchoNET.Infrastructure.csproj", "BanchoNET.Infrastructure/"]
COPY ["BanchoNET.Handlers.Stable/BanchoNET.Handlers.Stable.csproj", "BanchoNET.Handlers.Stable/"]
RUN dotnet restore "BanchoNET/BanchoNET.csproj" /p:Configuration=$BUILD_CONFIGURATION /p:PpProvider=$PP_PROVIDER
COPY . .
WORKDIR "/src/BanchoNET"
RUN dotnet build "./BanchoNET.csproj" --no-restore -c $BUILD_CONFIGURATION -o /app/build /p:PpProvider=$PP_PROVIDER

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG PP_PROVIDER=rosu
RUN dotnet publish "./BanchoNET.csproj" --no-restore -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:PpProvider=$PP_PROVIDER

FROM base AS final
WORKDIR /app

USER root

RUN mkdir -p /data/lazer \
    && chown -R "$APP_UID:$APP_UID" /data/lazer

ENV DOTNET_ROLL_FORWARD=Major

USER $APP_UID

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BanchoNET.dll"]
