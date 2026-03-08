# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY EncurtadorUrl.Api/EncurtadorUrl.Api.csproj EncurtadorUrl.Api/

COPY . .

RUN dotnet publish EncurtadorUrl.Api/EncurtadorUrl.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080

# Inicia a API
ENTRYPOINT ["dotnet", "EncurtadorUrl.Api.dll"]