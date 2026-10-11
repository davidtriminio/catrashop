#!/bin/sh
# Applies every sql/*.sql (idempotent) to the database in DATABASE_URL. Safe to run on every release.
set -eu
DIR="$(cd "$(dirname "$0")/.." && pwd)/sql"
: "${DATABASE_URL:?DATABASE_URL is required (see /etc/commercehub/commercehub.env)}"
for f in "$DIR"/*.sql; do
  echo ">> $(basename "$f")"
  psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -q -f "$f"
done
echo "Migrations applied."
