# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY TaxRefund.Api.csproj ./
RUN dotnet restore "TaxRefund.Api.csproj"

COPY . .
RUN dotnet publish "TaxRefund.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5158
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5158

HEALTHCHECK --interval=30s --timeout=5s --start-period=25s --retries=3 \
    CMD curl -fsS http://127.0.0.1:5158/health || exit 1

ENTRYPOINT ["dotnet", "TaxRefund.Api.dll"]
