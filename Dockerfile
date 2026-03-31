# Stage 1: Build React SPA
FROM node:22-alpine AS client-build
WORKDIR /app/client

COPY src/frontend/package.json src/frontend/package-lock.json ./
RUN npm ci

COPY src/frontend/ ./
RUN npm run build

# Stage 2: Build .NET API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src

ARG TARGETARCH

COPY src/GasTracker/Directory.Packages.props ./
COPY src/GasTracker/GasTracker.Core/GasTracker.Core.csproj src/GasTracker/GasTracker.Core/
COPY src/GasTracker/GasTracker.Infrastructure/GasTracker.Infrastructure.csproj src/GasTracker/GasTracker.Infrastructure/
COPY src/GasTracker/GasTracker.Api/GasTracker.Api.csproj src/GasTracker/GasTracker.Api/
RUN case ${TARGETARCH} in \
      amd64) DOTNET_ARCH=x64 ;; \
      arm64) DOTNET_ARCH=arm64 ;; \
      *) echo "Unsupported: ${TARGETARCH}" >&2; exit 1 ;; \
    esac && \
    dotnet restore src/GasTracker/GasTracker.Api/GasTracker.Api.csproj -r linux-${DOTNET_ARCH}

COPY src/GasTracker/ src/GasTracker/

RUN case ${TARGETARCH} in \
      amd64) DOTNET_ARCH=x64 ;; \
      arm64) DOTNET_ARCH=arm64 ;; \
      *) echo "Unsupported: ${TARGETARCH}" >&2; exit 1 ;; \
    esac && \
    dotnet publish src/GasTracker/GasTracker.Api/GasTracker.Api.csproj \
      -c Release -o /app/publish --no-restore \
      -r linux-${DOTNET_ARCH}

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

LABEL org.opencontainers.image.title="Gas Tracker" \
      org.opencontainers.image.description="Personal fuel tracking API with React SPA" \
      org.opencontainers.image.source="https://github.com/mggarofalo/gas"

RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

WORKDIR /app

COPY --from=api-build /app/publish .
COPY --from=client-build /app/client/dist ./wwwroot/

COPY docker-entrypoint.sh .
RUN chmod +x docker-entrypoint.sh

RUN mkdir -p /secrets

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -sf http://localhost:8080/health || exit 1

ENTRYPOINT ["./docker-entrypoint.sh"]
