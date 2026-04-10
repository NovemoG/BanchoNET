FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BanchoNET/BanchoNET.csproj", "BanchoNET/"]
COPY ["BanchoNET.Core/BanchoNET.Core.csproj", "BanchoNET.Core/"]
COPY ["BanchoNET.Handlers.Lazer/BanchoNET.Handlers.Lazer.csproj", "BanchoNET.Handlers.Lazer/"]
COPY ["BanchoNET.Infrastructure/BanchoNET.Infrastructure.csproj", "BanchoNET.Infrastructure/"]
COPY ["BanchoNET.Handlers.Stable/BanchoNET.Handlers.Stable.csproj", "BanchoNET.Handlers.Stable/"]
RUN dotnet restore "BanchoNET/BanchoNET.csproj"
COPY . .
WORKDIR "/src/BanchoNET"
RUN dotnet build "./BanchoNET.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BanchoNET.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS tools
RUN dotnet tool install vpk --tool-path /opt/tools

FROM base AS final
WORKDIR /app

USER root

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates zstd \
    && rm -rf /var/lib/apt/lists/*
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --version 8.0.100 --install-dir /opt/dotnet8 --no-path \
    && rm /tmp/dotnet-install.sh
RUN mkdir -p /data/lazer \
    && chown -R "$APP_UID:$APP_UID" /data/lazer

ENV DOTNET_ROLL_FORWARD=Major

USER $APP_UID

COPY --from=tools /opt/tools /opt/tools
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BanchoNET.dll"]
