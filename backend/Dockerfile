# syntax=docker/dockerfile:1
# Build context: repository root.  docker build -f backend/Dockerfile --target api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/ backend/
RUN dotnet publish backend/src/Api/Catrashop.Api/Catrashop.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- API runtime -------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
# Transfer receipts live here; created and owned by the app user so a named volume mounted on it is writable.
RUN mkdir -p /var/lib/catrashop/receipts && chown -R $APP_UID /var/lib/catrashop
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Catrashop.Api.dll"]

# --- Migrator (runs `dotnet ef database update` for every module, then exits) ------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS migrator
WORKDIR /src
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef
COPY backend/ backend/
COPY deploy/docker/migrate.sh /migrate.sh
RUN sed -i 's/\r$//' /migrate.sh && chmod +x /migrate.sh
ENTRYPOINT ["/migrate.sh"]
