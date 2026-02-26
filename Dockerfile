# ── Base runtime image ────────────────────────────────────────
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# ── Build stage ───────────────────────────────────────────────
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/ChaosWebApp/ChaosWebApp.csproj", "src/ChaosWebApp/"]
RUN dotnet restore "src/ChaosWebApp/ChaosWebApp.csproj"

COPY . .
WORKDIR "/src/src/ChaosWebApp"
RUN dotnet build "ChaosWebApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ── Publish stage ─────────────────────────────────────────────
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ChaosWebApp.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# ── Final image ───────────────────────────────────────────────
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Non-root user for security (.NET 8+ images include a built-in 'app' user)
USER app

ENTRYPOINT ["dotnet", "ChaosWebApp.dll"]
