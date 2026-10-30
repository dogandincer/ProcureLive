#!/usr/bin/env bash
# Tohum anlık görüntüsünü geri yükler (önce API'yi durdurur), Redis'i temizler, bekleyen migration'ları uygular.
# Anlık görüntü: konteynerde /tmp/seeded.dump (yedeği qa/data/seeded.dump).
set -euo pipefail
QA="$(cd "$(dirname "$0")/.." && pwd)"
bash "$QA/scripts/stop_api.sh"
export MSYS_NO_PATHCONV=1
docker exec procurelive-postgres test -f /tmp/seeded.dump || docker cp "$QA/data/seeded.dump" procurelive-postgres:/tmp/seeded.dump
docker exec procurelive-postgres psql -q -U procurelive_user -d postgres -c "DROP DATABASE procurelive WITH (FORCE);" -c "CREATE DATABASE procurelive OWNER procurelive_user;"
docker exec procurelive-postgres pg_restore -U procurelive_user -d procurelive --no-owner /tmp/seeded.dump
docker exec procurelive-redis redis-cli FLUSHALL > /dev/null
cd "$QA/../backend"
dotnet ef database update --no-build --project src/Infrastructure --startup-project src/WebApi 2>&1 | grep -E "Applying|error" || true
echo "restored: $(docker exec procurelive-postgres psql -tA -U procurelive_user -d procurelive -c 'select count(*) from "PurchaseOrders";') sipariş"
# API'yi arka planda yeniden başlat ve hazır olmasını bekle
(cd "$QA/../backend" && dotnet run --project src/WebApi --no-build --launch-profile http > "$QA/out/api.log" 2>&1 &)
for i in $(seq 1 90); do curl -s http://localhost:5228/health 2>/dev/null | grep -q Healthy && break; sleep 1; done
echo "api: $(curl -s http://localhost:5228/health)"
