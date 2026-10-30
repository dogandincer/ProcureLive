#!/usr/bin/env bash
# Mevcut veritabanını yeni tohum anlık görüntüsü olarak kaydeder (konteyner + qa/data yedeği).
set -euo pipefail
QA="$(cd "$(dirname "$0")/.." && pwd)"
export MSYS_NO_PATHCONV=1
docker exec procurelive-postgres pg_dump -U procurelive_user -d procurelive -Fc -f /tmp/seeded.dump
docker cp procurelive-postgres:/tmp/seeded.dump "$QA/data/seeded.dump"
ls -la "$QA/data/seeded.dump"
