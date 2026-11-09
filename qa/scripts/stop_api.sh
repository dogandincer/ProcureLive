#!/usr/bin/env bash
# API'yi durdurur: 5228'i dinleyen süreç + port alamamış ama arka plan servisleri (ihale zamanlayıcısı) DB'ye yazmaya devam eden
# artık ProcureLive.WebApi süreçleri. Bunlar kalırsa geri yükleme sırasında ihaleleri kendiliğinden başlatıp testleri bozar.
# (MSYS_NO_PATHCONV açıkken taskkill //F çalışmaz.)
unset MSYS_NO_PATHCONV
for pid in $(netstat -ano | grep -E ":5228 .*LISTEN" | awk '{print $5}' | sort -u); do taskkill //F //PID "$pid" > /dev/null 2>&1 || true; done
for pid in $(tasklist //FI "IMAGENAME eq ProcureLive.WebApi.exe" //FO CSV //NH 2>/dev/null | cut -d, -f2 | tr -d '"' | grep -E '^[0-9]+$'); do taskkill //F //PID "$pid" > /dev/null 2>&1 || true; done
sleep 1
