#!/usr/bin/env bash
# 5228'de dinleyen API sürecini durdurur. (MSYS_NO_PATHCONV açıkken taskkill //F çalışmaz.)
unset MSYS_NO_PATHCONV
for pid in $(netstat -ano | grep -E ":5228 .*LISTEN" | awk '{print $5}' | sort -u); do taskkill //F //PID "$pid" > /dev/null 2>&1 || true; done
sleep 1
