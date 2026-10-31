#!/bin/bash
# One-time server preparation (Ubuntu 24.04). Run as root from the unpacked release:  sudo bash server/install.sh
# Idempotent. Does NOT touch your existing Apache sites or APIs; it only adds its own vhost file (disabled).
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"

id catrashop >/dev/null 2>&1 || useradd --system --home /opt/catrashop --shell /usr/sbin/nologin catrashop
install -d -o catrashop -g catrashop /opt/catrashop /opt/catrashop/releases /var/log/catrashop
install -d -m 750 -o catrashop -g catrashop /var/lib/catrashop /var/lib/catrashop/receipts
install -d -m 750 -o root -g catrashop /etc/catrashop

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
install -m 644 "$HERE/apache-api.conf" /etc/apache2/sites-available/catrashop-api.conf

install -m 755 "$HERE/update.sh"  /opt/catrashop/update.sh
install -m 755 "$HERE/migrate.sh" /opt/catrashop/migrate.sh
install -m 644 "$HERE/catrashop-api.service" /etc/systemd/system/catrashop-api.service
[ -f /etc/catrashop/catrashop.env ] || install -m 640 -o root -g catrashop "$HERE/catrashop.env.example" /etc/catrashop/catrashop.env
systemctl daemon-reload
systemctl enable catrashop-api >/dev/null

echo
echo "Next steps (see docs/deployment.md):"
echo " 1. Create the database role + DB in your existing PostgreSQL."
echo " 2. Edit /etc/catrashop/catrashop.env (replace every CHANGE_ME_HEX with: openssl rand -hex 32)."
echo " 3. Add the Cloudflare Origin Certificate, then: a2ensite catrashop-api && apache2ctl configtest && systemctl reload apache2"
echo " 4. Deploy: sudo /opt/catrashop/update.sh /tmp/catrashop-release.tar.gz"
