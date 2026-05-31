# === STAGE 1: RUNTIME BASE ===
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# === STAGE 2: BUILD ENGINE ===
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first to leverage Docker cache layers for dependencies
COPY ["Saas.Api/Saas.Api.csproj", "Saas.Api/"]
COPY ["Saas.Appplication/Saas.Appplication.csproj", "Saas.Appplication/"]
COPY ["Saas.Domain/Saas.Domain.csproj", "Saas.Domain/"]
COPY ["Saas.Infrastructure/Saas.Infrastructure.csproj", "Saas.Infrastructure/"]

# Restore all clean dependencies across the solution layers
RUN dotnet restore "Saas.Api/Saas.Api.csproj"

# Copy the rest of the source code files
COPY . .
WORKDIR "/src/Saas.Api"
RUN dotnet build "Saas.Api.csproj" -c Release -o /app/build

# === STAGE 3: PUBLISH OPTIMIZATION ===
FROM build AS publish
RUN dotnet publish "Saas.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# === STAGE 4: FINAL CLEAN RUNTIME ===
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Saas.Api.dll"]