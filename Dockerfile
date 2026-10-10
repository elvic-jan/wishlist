# Single production image: the ASP.NET Core API serves the built SPA from
# wwwroot, so the browser talks to one origin. TLS terminates at the platform
# ingress; the container listens on plain HTTP 8080.

# --- SPA build ---------------------------------------------------------------
FROM node:24-alpine AS spa
WORKDIR /src/Wishlist.Web
COPY src/Wishlist.Web/package.json src/Wishlist.Web/package-lock.json ./
RUN npm ci
COPY src/Wishlist.Web/ ./
RUN npm run build

# --- API publish -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS api
WORKDIR /src
# Restore on project files alone so the layer is cached across code changes.
COPY src/Wishlist.Domain/Wishlist.Domain.csproj src/Wishlist.Domain/
COPY src/Wishlist.Application/Wishlist.Application.csproj src/Wishlist.Application/
COPY src/Wishlist.Infrastructure/Wishlist.Infrastructure.csproj src/Wishlist.Infrastructure/
COPY src/Wishlist.API/Wishlist.API.csproj src/Wishlist.API/
RUN dotnet restore src/Wishlist.API/Wishlist.API.csproj
COPY src/ src/
RUN dotnet publish src/Wishlist.API/Wishlist.API.csproj -c Release --no-restore -o /app
COPY --from=spa /src/Wishlist.Web/dist/ /app/wwwroot/

# --- Runtime -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=api /app ./
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Wishlist.API.dll"]
