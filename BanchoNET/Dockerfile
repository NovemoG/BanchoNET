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

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BanchoNET.dll"]
