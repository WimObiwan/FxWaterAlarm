# Retire blog.wateralarm.be (switch `/` to the Site frontpage)

One-off procedure for server3, to run **after** the Site release that contains the new
`Site/Pages/Index.cshtml` is deployed. Background:
[`../decisions/single-site.md`](../decisions/single-site.md).

Steps 0–2 and 4 were done on 2026-09-27. The files are `/etc/nginx/conf.d/wateralarm.be.conf`
(`www.wateralarm.be` 443 block) and `/etc/nginx/conf.d/blog.wateralarm.be.conf`. Only steps 3
(optional) and 5 are left.

## 0. Look

```bash
ssh root@server3.foxinnovations.be
grep -rn "blog.wateralarm.be\|wateralarm.be" /etc/nginx/sites-enabled/ /etc/nginx/conf.d/
```

Find:
- the `www.wateralarm.be` server block, and the rule that sends `/` to the blog (a
  `location = /` with `return 301 https://blog.wateralarm.be/...`, or similar);
- the `blog.wateralarm.be` server block and its webroot (the Mobirise files).

Back up every file you are about to change: `cp <file> <file>.bak-$(date +%F)`.

## 1. Serve `/` from the app

In the `www.wateralarm.be` block, remove the `/` → blog redirect so that `/` goes to Kestrel
like every other path.

## 2. Redirect the blog subdomain

Replace the body of the `blog.wateralarm.be` server block (keep `listen` and the
certificate lines):

```nginx
server {
    server_name blog.wateralarm.be;
    # ... existing listen / ssl_certificate lines stay ...
    return 301 https://www.wateralarm.be$request_uri;
}
```

`$request_uri` keeps the path and the query string, so UTM tags on old QR codes and printed
material survive. (The old www → blog redirect dropped them.)

Keep the certificate renewing for `blog.wateralarm.be` as long as old links may be around.

## 3. Optional: static fallback when Kestrel is down

In the `www.wateralarm.be` block:

```nginx
error_page 502 503 504 /offline.html;
location = /offline.html {
    root /var/www/wateralarm-offline;
    internal;
}
```

with a small `offline.html` carrying the contact details (info@wateralarm.be, 0484 24 84 24).

## 4. Apply and verify

```bash
nginx -t && systemctl reload nginx

curl -sI https://www.wateralarm.be/ | grep -iE '^HTTP|^location'                 # 200, no location
curl -sI 'https://blog.wateralarm.be/?utm_source=x' | grep -iE '^HTTP|^location'  # 301 -> https://www.wateralarm.be/?utm_source=x
curl -sI https://wateralarm.be/ | grep -iE '^HTTP|^location'                     # 301 -> https://www.wateralarm.be/
curl -sI 'https://www.wateralarm.be/Short?c=qr|pst|th25|111' | grep -i '^location'  # /?utm_source=qr&...
```

Also open `https://www.wateralarm.be/` in a browser: once without cookies (Live demo /
Inloggen), and once after opening your own sensor link (Mijn sensoren).

## 5. Clean up (a few weeks later)

- Archive the Mobirise webroot, then remove it from server3.
- Stop GA property `G-HEJ2ZM9CYB` (the blog's) once it no longer receives traffic.
- Update [`../state/2026-09-27-frontpage-merge.md`](../state/2026-09-27-frontpage-merge.md)
  with a dated addendum.
