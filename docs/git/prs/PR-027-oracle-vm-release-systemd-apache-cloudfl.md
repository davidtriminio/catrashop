# PR-027 — Oracle VM release (systemd + Apache + Cloudflare) and Vercel config

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/oracle-deploy` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
A repeatable, tiny deployment for the Oracle VM (Ubuntu 24.04, 1 GB RAM, no Docker) and the Vercel frontend config.

## Changes
- `deploy/oracle`: `build-release.ps1` (~4 MB bundle with idempotent SQL migrations), `install.sh`, `update.sh` (health check + rollback), systemd unit, Apache vhost behind Cloudflare.
- `frontend/vercel.json`: proxies `/api/*` to the API and falls back to `index.html`.
- The Docker stack moves to `deploy/docker` for bigger hosts.

## Verification
Release bundle builds; the generated SQL applies cleanly to an empty PostgreSQL.
