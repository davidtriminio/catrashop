# Deployment & secrets

Target topology:

```
Browser ──► Vercel (Angular web + admin) ──/api/* rewrite──►┐
Flutter admin app ──────────────────────────────────────────┤
                                                             ▼
                        Cloudflare (api.davidtriminio.dev, proxied, Full strict)
                                                             ▼
                        Oracle VM (Ubuntu 24.04, 1 OCPU / 1 GB / ~5 GB free)
                          Apache :443 ─► Kestrel 127.0.0.1:5080 (systemd) ─► PostgreSQL (existing)
```

Because Vercel proxies `/api/*` to the API (`frontend/vercel.json`), the Angular apps keep using relative
`/api/...` URLs and no CORS setup is needed for them. The mobile app calls `https://api.davidtriminio.dev` directly.

## Local development (no secrets in the repo)
Secrets live in `.env` (gitignored, for docker compose) and in .NET User Secrets (outside the repo):

```bash
cd backend/src/Api/CommerceHub.Api
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=commercehub;Username=...;Password=..."
dotnet user-secrets set "Identity:Jwt:SigningKey" "$(openssl rand -base64 48)"
dotnet user-secrets set "Payments:WebhookSecret" "$(openssl rand -base64 32)"
dotnet user-secrets set "RabbitMq:Password" "..."
```
For `dotnet ef` design-time commands set `COMMERCEHUB_CONNECTION` (falls back to a placeholder).

## Production on the Oracle VM (1 GB RAM, no Docker)

Memory budget: the API is capped at ~450 MB (`MemoryMax`), RabbitMQ is **not** used
(`Messaging__Transport=InProcess` — the transactional Outbox still guarantees at-least-once delivery), and the
release bundle is ~4 MB because it is framework-dependent and nothing is compiled on the server.

### One-time setup
1. **Build** on your PC: `./deploy/oracle/build-release.ps1` → `deploy/oracle/dist/commercehub-release.tar.gz`
2. **Upload**: `scp commercehub-release.tar.gz ubuntu@<vm>:/tmp/` and unpack the `server/` folder once:
   `mkdir /tmp/ch && tar -xzf /tmp/commercehub-release.tar.gz -C /tmp/ch && sudo bash /tmp/ch/server/install.sh`
   (creates the `commercehub` user, 1 GB swap if missing, systemd unit, Apache modules and a *disabled* vhost; it
   does not touch your existing Apache sites or APIs).
3. **Database** (your existing PostgreSQL):
   `sudo -u postgres psql -c "CREATE ROLE commercehub LOGIN PASSWORD '<openssl rand -hex 24>';" -c "CREATE DATABASE commercehub OWNER commercehub;"`
4. **Secrets**: edit `/etc/commercehub/commercehub.env` and replace every `CHANGE_ME_HEX` (`openssl rand -hex 32`).
5. **Cloudflare**: DNS `A api → VM public IP` (proxied); SSL/TLS mode **Full (strict)**; create an *Origin Certificate*
   and save it as `/etc/ssl/cloudflare/davidtriminio.dev.pem` / `.key` (key `chmod 600`).
   Open only 80/443 (and 22) in the Oracle security list and `ufw`.
6. `sudo a2ensite commercehub-api && sudo apache2ctl configtest && sudo systemctl reload apache2`
7. First deploy: `sudo /opt/commercehub/update.sh /tmp/commercehub-release.tar.gz`

### Every release
```powershell
./deploy/oracle/build-release.ps1 -Upload -Server ubuntu@<vm> -Key ~/.ssh/oracle.key
ssh ubuntu@<vm> sudo /opt/commercehub/update.sh /tmp/commercehub-release.tar.gz
```
`update.sh` unpacks, applies the idempotent SQL migrations (`psql`), switches the `current` symlink, restarts,
checks `/health/live`, rolls back automatically on failure and keeps only the last 2 releases (disk).

Verify: `curl https://api.davidtriminio.dev/health/ready` and `journalctl -u commercehub-api -f`.

### Create the first admin
Register a normal account (`POST /api/v1/identity/register`), then promote it once in the database:

```sql
INSERT INTO identity.user_roles (user_id, role)
SELECT id, 'Administrator' FROM identity.users WHERE email = 'you@example.com'
ON CONFLICT DO NOTHING;
```
Log out/in to get a token carrying the new role, then use the admin SPA or the mobile app.
Use `'OrderManager'` instead for staff who should only manage orders.

## Frontend on Vercel
Create **two Vercel projects** from the same repo, both with *Root Directory* `frontend`:

| Project | Build command | Output directory |
|---|---|---|
| web (customer) | `pnpm exec ng build commercehub-web` | `dist/commercehub-web/browser` |
| admin | `pnpm exec ng build commercehub-admin` | `dist/commercehub-admin/browser` |

`frontend/vercel.json` is shared: it proxies `/api/*` to the API and falls back to `index.html` for SPA routes.

## Alternative: Docker (bigger hosts only)
`deploy/docker/` holds a full compose stack (Postgres, RabbitMQ, API, nginx, Caddy). It needs well over 1 GB RAM
and several GB of disk, so it is **not** the path for the Oracle micro VM:
`docker compose -f deploy/docker/docker-compose.prod.yml --env-file deploy/docker/.env.prod up -d --build`
