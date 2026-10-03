#!/bin/bash
# Deploys a release tarball: unpack, migrate DB, switch the `current` symlink, restart, health check, roll back on failure.
set -euo pipefail
TAR="${1:?usage: update.sh /path/to/commercehub-release.tar.gz}"
BASE=/opt/commercehub
REL="$BASE/releases/$(date +%Y%m%d%H%M%S)"
PREV="$(readlink -f "$BASE/current" 2>/dev/null || true)"

mkdir -p "$REL" && tar -xzf "$TAR" -C "$REL" && chown -R commercehub:commercehub "$REL"
set -a; . /etc/commercehub/commercehub.env; set +a
sh "$REL/server/migrate.sh"

ln -sfn "$REL/app" "$BASE/current"
systemctl restart commercehub-api
for _ in $(seq 1 30); do
  if curl -fsS http://127.0.0.1:5080/health/live >/dev/null 2>&1; then
    echo "API is live. Release: $REL"
    # keep only the last 2 releases to save disk
    ls -1dt "$BASE"/releases/* | tail -n +3 | xargs -r rm -rf
    rm -f "$TAR"
    exit 0
  fi
  sleep 2
done

echo "Health check failed - rolling back" >&2
if [ -n "$PREV" ]; then ln -sfn "$PREV" "$BASE/current"; systemctl restart commercehub-api; fi
journalctl -u commercehub-api -n 40 --no-pager >&2
exit 1
