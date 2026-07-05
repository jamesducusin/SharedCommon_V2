FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["global.json", "."]

# Copy project files for layer caching
COPY ["src/", "src/"]

RUN dotnet restore "src/SharedCommon.Core/SharedCommon.Core.csproj" --no-cache

COPY . .
RUN dotnet build "src/SharedCommon.Core/SharedCommon.Core.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "src/SharedCommon.Core/SharedCommon.Core.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SharedCommon.Core.dll"]
