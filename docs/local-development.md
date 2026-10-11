# Local development — API, seed data, web and mobile

Everything below runs on your PC. Commands are PowerShell, run from the repository root.

## 0. What you need
PostgreSQL (local or Docker), .NET 10 SDK + `dotnet tool install -g dotnet-ef`, Node 22 + pnpm, Flutter, and `psql`
on the PATH (it ships with PostgreSQL). RabbitMQ is optional: without it set `Messaging__Transport=InProcess`
(see step 2).

## 1. Secrets (once)
Dev secrets live in .NET user-secrets, outside the repo. Replace the values in `<>`:

```powershell
$api = "backend/src/Api/CommerceHub.Api"
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=commercehub;Username=postgres;Password=<your-postgres-password>" --project $api
dotnet user-secrets set "Identity:Jwt:SigningKey" "<at-least-32-random-chars>" --project $api
dotnet user-secrets set "Payments:WebhookSecret" "<at-least-16-random-chars>" --project $api
# only if you run RabbitMQ:
dotnet user-secrets set "RabbitMq:UserName" "guest" --project $api
dotnet user-secrets set "RabbitMq:Password" "<rabbit-password>" --project $api
dotnet user-secrets list --project $api
```
Generate a random value with: `[Convert]::ToBase64String((1..48 | % { Get-Random -Max 256 }) -as [byte[]])`

## 2. Database + API
```powershell
./scripts/local/setup-db.ps1                 # creates the DB if missing and applies every module's migrations
dotnet run --project backend/src/Api/CommerceHub.Api          # http://localhost:5227
# no RabbitMQ running?  $env:Messaging__Transport = "InProcess"  before dotnet run
```
Check: `Invoke-RestMethod http://localhost:5227/health/live`

## 3. Seed (admin user, catalog, stock, customer, 4 orders)
With the API running, in another terminal:
```powershell
./scripts/local/seed.ps1
```
It registers the users through the API, promotes the admin with one SQL `INSERT` (no endpoint exists for that),
creates 3 published products with stock, and places 4 orders: 2 `Paid`, 1 `Processing`, 1 `Shipped` — so every
button in the app has something to act on. Local test accounts it prints (never reuse them anywhere real):

| Role | Email | Password |
|---|---|---|
| Administrator | `admin@commercehub.local` | `Admin12345` |
| Customer | `cliente@commercehub.local` | `Cliente12345` |

Custom values: `./scripts/local/seed.ps1 -AdminEmail you@x.dev -AdminPassword 'YourPass123'` (min. 8 chars with
upper, lower and a digit). Re-running is safe for users/catalog and just adds 4 more orders.

Create more orders later by hand: log in as the customer, `POST /api/v1/cart/items`, `POST /api/v1/orders/checkout`,
`POST /api/v1/payments` (with an `Idempotency-Key` header) — see [demo-data.md](demo-data.md).

## 4. Angular apps
```powershell
cd frontend; pnpm install
pnpm start                         # customer SPA  http://localhost:4200  (proxies /api to :5227)
pnpm exec ng serve commercehub-admin   # admin SPA     http://localhost:4300
```
Log in to the admin SPA with the admin account.

## 5. Flutter app
```powershell
cd mobile; flutter pub get; flutter devices
```
Pick how the phone reaches the API on your PC (debug builds allow plain HTTP):

| Target | Command |
|---|---|
| Android emulator | `flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5227` |
| Phone on USB (debugging on) | `adb reverse tcp:5227 tcp:5227` then `flutter run --dart-define=API_BASE_URL=http://localhost:5227` |
| Phone on Wi-Fi | run the API with `dotnet run --project backend/src/Api/CommerceHub.Api --urls http://0.0.0.0:5227`, allow it in Windows Firewall (`New-NetFirewallRule -DisplayName CommerceHubDev -Direction Inbound -LocalPort 5227 -Protocol TCP -Action Allow`), then `flutter run --dart-define=API_BASE_URL=http://<PC-LAN-IP>:5227` |

Log in with the **admin** account. A customer account is rejected on purpose ("no tiene permisos").

Try it: filter by *Pagado*, open an order, press *Iniciar proceso* → *Marcar enviado* → *Marcar entregado*.
To see the new-order alert, keep the app open and place another order as the customer (step 3's last bullet): within
~60 s a notification appears. The background check (app closed) runs about every 15 min and Android may delay it.
The biometric lock needs a screen lock or fingerprint set on the device/emulator
(emulator: *Settings → Security → Screen lock*, and add a fingerprint under *Extended controls → Fingerprint*).

## 6. Tests
```powershell
dotnet test backend/CommerceHub.slnx
cd mobile; flutter test; flutter analyze
```
`HealthAndRootTests.Readiness_reports_...infra_is_down` assumes Postgres/RabbitMQ are **not** running; it fails
while your local infrastructure is up. That is expected on a dev machine with the stack running.

## Reset
```powershell
# wipes all data (keeps no seed) — then repeat steps 2-3
psql -h localhost -U postgres -c "DROP DATABASE commercehub WITH (FORCE)"
```
