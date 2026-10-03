#!/bin/bash
# One-time server preparation (Ubuntu 24.04). Run as root from the unpacked release:  sudo bash server/install.sh
# Idempotent. Does NOT touch your existing Apache sites or APIs; it only adds its own vhost file (disabled).
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"

id commercehub >/dev/null 2>&1 || useradd --system --home /opt/commercehub --shell /usr/sbin/nologin commercehub
install -d -o commercehub -g commercehub /opt/commercehub /opt/commercehub/releases /var/log/commercehub
install -d -m 750 -o root -g commercehub /etc/commercehub

# 1 GB RAM: make sure there is a swap file (uses 1 GB of the disk)
if ! swapon --show | grep -q .; then
  fallocate -l 1G /swapfile && chmod 600 /swapfile && mkswap /swapfile && swapon /swapfile
  grep -q '^/swapfile' /etc/fstab || echo '/swapfile none swap sw 0 0' >> /etc/fstab
fi

dotnet --list-runtimes | grep -q 'Microsoft.AspNetCore.App 10\.' || \
  echo "WARNING: ASP.NET Core 10 runtime not found (apt-get install -y aspnetcore-runtime-10.0)"
command -v psql >/dev/null || apt-get install -y postgresql-client

# Apache modules, Cloudflare IP ranges, vhost (enable it only after adding the origin certificate)
a2enmod proxy proxy_http ssl headers remoteip >/dev/null
{ curl -fsS https://www.cloudflare.com/ips-v4; echo; curl -fsS https://www.cloudflare.com/ips-v6; } | sed '/^$/d' > /etc/apache2/cloudflare-ips.txt
install -m 644 "$HERE/apache-api.conf" /etc/apache2/sites-available/commercehub-api.conf

install -m 755 "$HERE/update.sh"  /opt/commercehub/update.sh
install -m 755 "$HERE/migrate.sh" /opt/commercehub/migrate.sh
install -m 644 "$HERE/commercehub-api.service" /etc/systemd/system/commercehub-api.service
[ -f /etc/commercehub/commercehub.env ] || install -m 640 -o root -g commercehub "$HERE/commercehub.env.example" /etc/commercehub/commercehub.env
systemctl daemon-reload
systemctl enable commercehub-api >/dev/null

echo
echo "Next steps (see docs/deployment.md):"
echo " 1. Create the database role + DB in your existing PostgreSQL."
echo " 2. Edit /etc/commercehub/commercehub.env (replace every CHANGE_ME_HEX with: openssl rand -hex 32)."
echo " 3. Add the Cloudflare Origin Certificate, then: a2ensite commercehub-api && apache2ctl configtest && systemctl reload apache2"
echo " 4. Deploy: sudo /opt/commercehub/update.sh /tmp/commercehub-release.tar.gz"
