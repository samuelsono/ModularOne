# Ubuntu deployment (systemd, nginx, PostgreSQL, Redis)

Production layout for CarTrack on Ubuntu 22.04/24.04. Nginx terminates TLS and proxies to Kestrel. PostgreSQL and Redis run on the same host (or reachable private hosts). Application secrets and connection strings live in:

```text
/etc/cartrack/config.env
```

| Path | Purpose |
|------|---------|
| `/opt/cartrack/current` | Published .NET host + SPA `wwwroot` |
| `/etc/cartrack/config.env` | Environment file loaded by systemd |
| `/var/log/cartrack` | Optional app log directory |
| `/etc/nginx/sites-available/cartrack` | Nginx site (symlink into `sites-enabled`) |
| `/etc/systemd/system/cartrack.service` | systemd unit |

Replace `app.example.com` with your public hostname before enabling the site.

## 1. Packages

```bash
sudo apt update
sudo apt install -y nginx postgresql redis-server certbot python3-certbot-nginx
```

Install the .NET 10 ASP.NET Core runtime (or hosting bundle) from [Microsoft’s Ubuntu packages](https://learn.microsoft.com/dotnet/core/install/linux-ubuntu).

## 2. PostgreSQL

See [postgres/setup.md](./postgres/setup.md). If startup fails with `AspNetRoles already exists`, follow [postgres/migration-recovery.md](./postgres/migration-recovery.md).

## 3. Redis

See [redis/redis.conf.snippet](./redis/redis.conf.snippet). Bind to localhost and require a password in production.

## 4. Application user and publish directory

```bash
sudo useradd --system --home /opt/cartrack --shell /usr/sbin/nologin cartrack
sudo mkdir -p /opt/cartrack/current /etc/cartrack /var/log/cartrack
sudo chown -R cartrack:cartrack /opt/cartrack /var/log/cartrack
sudo chmod 750 /etc/cartrack
```

Publish from CI or a build host:

```bash
dotnet publish CarTrack.Host/CarTrack.Host.csproj -c Release -o /opt/cartrack/current
# SPA assets must land in /opt/cartrack/current/wwwroot (Aspire PublishWithContainerFiles or a separate frontend build copy).
```

## 5. Environment file

```bash
sudo cp docs/deploy/config.env.example /etc/cartrack/config.env
sudo chown root:cartrack /etc/cartrack/config.env
sudo chmod 640 /etc/cartrack/config.env
sudoedit /etc/cartrack/config.env
```

systemd injects these variables via `EnvironmentFile=` before the process starts. Nested ASP.NET Core settings use `__` (for example `Jwt__Secret`, `ConnectionStrings__cartrack`).

## 6. systemd

```bash
sudo cp docs/deploy/systemd/cartrack.service /etc/systemd/system/cartrack.service
sudo systemctl daemon-reload
sudo systemctl enable --now cartrack
sudo systemctl status cartrack
```

## 7. nginx + TLS

```bash
sudo cp docs/deploy/nginx/cartrack.conf /etc/nginx/sites-available/cartrack
# Edit server_name and certificate paths (or obtain certs first).
sudo ln -sf /etc/nginx/sites-available/cartrack /etc/nginx/sites-enabled/cartrack
sudo nginx -t
sudo systemctl reload nginx

sudo certbot --nginx -d app.example.com
```

The site config includes the HTTP security headers Snyk’s website security score expects (same set graded by [securityheaders.com](https://securityheaders.com)):

- `Strict-Transport-Security`
- `Content-Security-Policy`
- `X-Frame-Options`
- `X-Content-Type-Options`
- `X-XSS-Protection`
- `Referrer-Policy`
- `Permissions-Policy`

Verify after go-live:

```bash
curl -sI https://app.example.com | grep -iE 'strict-transport|content-security|x-frame|x-content-type|x-xss|referrer-policy|permissions-policy'
```

Then re-scan the public URL in Snyk / securityheaders.com. Tune `Content-Security-Policy` if a third-party script, font, or API origin is required.

## 8. Health check

```bash
curl -fsS http://127.0.0.1:5000/health
curl -fsS https://app.example.com/health
```

## File map

```text
docs/deploy/
  README.md
  config.env.example
  systemd/cartrack.service
  nginx/cartrack.conf
  postgres/setup.md
  redis/redis.conf.snippet
```
