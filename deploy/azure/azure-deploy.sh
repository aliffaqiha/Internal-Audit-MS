#!/usr/bin/env bash
#
# Deploy the IAMS backend to Azure Container Apps and print the Vercel
# environment variables to connect the frontend.
#
# Usage:
#   cp deploy/azure/.env.example deploy/azure/.env   # then fill it in
#   az login
#   bash deploy/azure/azure-deploy.sh
#
# Required CLI: Azure CLI (az). Run from the repository root, or the script
# will locate the root automatically.
#
# NOTE: This targets a fresh setup. It is idempotent for the main resources,
# but re-running after a partially successful run is safe because each step
# that can fail on an existing resource is guarded.

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"

# ---------------------------------------------------------------------------
# Load configuration
# ---------------------------------------------------------------------------
ENV_FILE="$SCRIPT_DIR/.env"
if [[ -f "$ENV_FILE" ]]; then
  set -a
  # shellcheck disable=SC1090
  source "$ENV_FILE"
  set +a
fi

REQUIRED=(
  AZURE_LOCATION RESOURCE_GROUP
  PG_SERVER PG_DB PG_USER PG_PASSWORD
  REDIS_NAME ACR_NAME
  CAE_NAME CA_API_NAME CA_MINIO_NAME
  MINIO_USER MINIO_PASSWORD
  JWT_SECRET_KEY
  SEED_ADMIN_PASSWORD
  CORS_ORIGIN CLIENT_BASE_URL
)
MISSING=()
for var in "${REQUIRED[@]}"; do
  if [[ -z "${!var:-}" ]]; then
    MISSING+=("$var")
  fi
done
if [[ ${#MISSING[@]} -gt 0 ]]; then
  echo "ERROR: missing required variable(s): ${MISSING[*]}"
  echo "  -> Fill deploy/azure/.env (copy from .env.example) or export them."
  exit 1
fi
if [[ ${#JWT_SECRET_KEY} -lt 32 ]]; then
  echo "ERROR: JWT_SECRET_KEY must be at least 32 characters."
  echo "  -> Generate one with: openssl rand -base64 48"
  exit 1
fi

log()  { echo; echo "==> $*"; }
done_() { echo "    [ok] $*"; }

cd "$REPO_ROOT"

# ---------------------------------------------------------------------------
# 0. Verify Azure CLI + subscription
# ---------------------------------------------------------------------------
log "Checking Azure CLI and subscription..."
az account show -o table
done_ "authenticated"

# ---------------------------------------------------------------------------
# 1. Resource group
# ---------------------------------------------------------------------------
log "Ensuring resource group '$RESOURCE_GROUP'..."
az group create --name "$RESOURCE_GROUP" --location "$AZURE_LOCATION" -o none

# ---------------------------------------------------------------------------
# 2. PostgreSQL Flexible Server + database
# ---------------------------------------------------------------------------
log "Ensuring PostgreSQL flexible server '$PG_SERVER'..."
if ! az postgres flexible-server show -g "$RESOURCE_GROUP" -n "$PG_SERVER" >/dev/null 2>&1; then
  az postgres flexible-server create \
    -g "$RESOURCE_GROUP" -n "$PG_SERVER" \
    --admin-user "$PG_USER" --admin-password "$PG_PASSWORD" \
    --sku-name Standard_B1ms --tier Burstable --storage-size 32 --version 16 \
    -o none
fi
done_ "postgres server"

log "Ensuring database '$PG_DB'..."
az postgres flexible-server db create -g "$RESOURCE_GROUP" -s "$PG_SERVER" -d "$PG_DB" -o none || true

log "Allowing Azure services through the Postgres firewall..."
az postgres flexible-server firewall-rule create \
  -g "$RESOURCE_GROUP" -n "$PG_SERVER" \
  --rule-name AllowAzure --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 \
  -o none || true

CONNECTION_STRING="Host=$PG_SERVER.postgres.database.azure.com;Port=5432;Database=$PG_DB;Username=$PG_USER;Password=$PG_PASSWORD;SslMode=Require"

# ---------------------------------------------------------------------------
# 3. Azure Cache for Redis
# ---------------------------------------------------------------------------
log "Ensuring Redis cache '$REDIS_NAME' (with non-SSL port)..."
if ! az redis show -g "$RESOURCE_GROUP" -n "$REDIS_NAME" >/dev/null 2>&1; then
  az redis create -g "$RESOURCE_GROUP" -n "$REDIS_NAME" \
    --sku Basic --vm-size c0 --enable-non-ssl-port \
    -o none
fi
REDIS_KEY="$(az redis list-keys -g "$RESOURCE_GROUP" -n "$REDIS_NAME" --query primaryKey -o tsv)"
REDIS_CONNECTION="$REDIS_NAME.redis.cache.windows.net:6379,password=$REDIS_KEY,ssl=False,abortConnect=False"

# ---------------------------------------------------------------------------
# 4. Container Apps environment
# ---------------------------------------------------------------------------
log "Ensuring Container Apps environment '$CAE_NAME'..."
az containerapp env create -g "$RESOURCE_GROUP" -n "$CAE_NAME" --location "$AZURE_LOCATION" -o none
ENV_FQDN="$(az containerapp env show -g "$RESOURCE_GROUP" -n "$CAE_NAME" --query properties.defaultDomain -o tsv)"
done_ "environment domain: $ENV_FQDN"

# ---------------------------------------------------------------------------
# 5. MinIO (internal ingress, reachable only from inside this environment)
#
# WARNING: by default the MinIO container has an ephemeral filesystem. To
# persist uploads across restarts, attach an Azure Files volume to /data
# (see `az containerapp env storage set` + `--bind-mount` in the az docs).
# ---------------------------------------------------------------------------
log "Ensuring MinIO container app '$CA_MINIO_NAME' (internal)..."
az containerapp create -g "$RESOURCE_GROUP" -n "$CA_MINIO_NAME" \
  --environment "$CAE_NAME" \
  --image minio/minio:latest \
  --ingress internal --target-port 9000 \
  --cpu 0.25 --memory 0.5Gi --min-replicas 1 --max-replicas 1 \
  --args "server /data --console-address :9001" \
  --env-vars \
    MINIO_ROOT_USER="$MINIO_USER" \
    MINIO_ROOT_PASSWORD="$MINIO_PASSWORD" \
  -o none
MINIO_ENDPOINT="$CA_MINIO_NAME.$ENV_FQDN:9000"
done_ "MinIO endpoint: $MINIO_ENDPOINT"

# ---------------------------------------------------------------------------
# 6. Build & push the API image to Azure Container Registry
# ---------------------------------------------------------------------------
log "Ensuring Container Registry '$ACR_NAME' and building image..."
if ! az acr show -g "$RESOURCE_GROUP" -n "$ACR_NAME" >/dev/null 2>&1; then
  az acr create -g "$RESOURCE_GROUP" -n "$ACR_NAME" --sku Basic --admin-enabled true -o none
fi
REGISTRY="$ACR_NAME.azurecr.io"
IMAGE="$REGISTRY/iams-api:v1"
az acr build --registry "$ACR_NAME" --image iams-api:v1 \
  --file src/Backend/API/Dockerfile . -o table
done_ "image $IMAGE"

# ---------------------------------------------------------------------------
# 7. Deploy the API
# ---------------------------------------------------------------------------
log "Deploying API container app '$CA_API_NAME'..."
ACR_PASSWORD="$(az acr credential show -g "$RESOURCE_GROUP" -n "$ACR_NAME" --query passwords[0].value -o tsv)"

az containerapp create -g "$RESOURCE_GROUP" -n "$CA_API_NAME" \
  --environment "$CAE_NAME" \
  --image "$IMAGE" \
  --registry-server "$REGISTRY" \
  --registry-username "$ACR_NAME" \
  --registry-password "$ACR_PASSWORD" \
  --ingress external --target-port 8080 \
  --cpu 0.5 --memory 1.0Gi --min-replicas 1 --max-replicas 1 \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ConnectionStrings__DefaultConnection="$CONNECTION_STRING" \
    ConnectionStrings__Redis="$REDIS_CONNECTION" \
    Jwt__SecretKey="$JWT_SECRET_KEY" \
    Jwt__SecureCookie=true \
    Minio__Endpoint="$MINIO_ENDPOINT" \
    Minio__AccessKey="$MINIO_USER" \
    Minio__SecretKey="$MINIO_PASSWORD" \
    Cors__Origins__0="$CORS_ORIGIN" \
    AppUrls__ClientBaseUrl="$CLIENT_BASE_URL" \
    SeedAdmin__Enabled=true \
    SeedAdmin__Username="$SEED_ADMIN_USERNAME" \
    SeedAdmin__Password="$SEED_ADMIN_PASSWORD" \
    SeedAdmin__Email="$SEED_ADMIN_EMAIL" \
    SeedAdmin__FullName="$SEED_ADMIN_FULLNAME" \
    SeedAdmin__MustChangePassword=true \
  -o none
done_ "API deployed"

API_URL="$(az containerapp show -g "$RESOURCE_GROUP" -n "$CA_API_NAME" --query properties.configuration.ingress.fqdn -o tsv)"
log "API URL (temporary): https://$API_URL"

# ---------------------------------------------------------------------------
# 8. Optional custom domain + managed TLS certificate
# ---------------------------------------------------------------------------
if [[ -n "${API_DOMAIN:-}" ]]; then
  log "Attaching custom domain '$API_DOMAIN'..."
  az containerapp hostname add -g "$RESOURCE_GROUP" -n "$CA_API_NAME" \
    --hostname "$API_DOMAIN" -o none || true
  az containerapp certificate create -g "$RESOURCE_GROUP" -n "$CA_API_NAME" \
    --hostname "$API_DOMAIN" --managed -o none \
    || echo "  -> Could not create the managed certificate automatically."
  echo "  -> Point DNS: CNAME '$API_DOMAIN' -> '$(echo "$API_URL" | sed 's/^https:\/\///')'"
  echo "     (or an ALIAS record). TLS will be provisioned once DNS resolves."
  API_BASE="https://$API_DOMAIN/api"
  HUB_BASE="https://$API_DOMAIN/hubs/notifications"
else
  API_BASE="https://$API_URL/api"
  HUB_BASE="https://$API_URL/hubs/notifications"
fi

# ---------------------------------------------------------------------------
# 9. Summary: connect Vercel
# ---------------------------------------------------------------------------
cat <<EOF

=== Deployment complete ===

1. On the API (Azure):
   - Check startup logs:  az containerapp logs show -g $RESOURCE_GROUP -n $CA_API_NAME
   - First boot runs EF migrations and seeds the admin user.

2. On Vercel (frontend):
   - Project settings -> Root Directory = src/ClientApp
   - Environment Variables:
       VITE_API_URL=$API_BASE
       VITE_HUB_URL=$HUB_BASE
   - Redeploy, then sign in with:
       username: ${SEED_ADMIN_USERNAME:-admin}
       password: (SeedAdmin__Password value)
EOF

log "Done."