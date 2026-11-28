# Game server: top-up payments and login (Cloudflare Workers)

One Worker (`payments/worker`) handles two things:

- **Top-up payments** with Midtrans Snap (sections 1–5 below)
- **Email login and cloud saves** (section 6 below)

# Top-up payments (Midtrans)

Real-money bundles for the WebGL build. Unity IAP doesn't support WebGL, so
payments go through **Midtrans Snap** with a small **Cloudflare Worker** that
holds the secret key, receives payment notifications and makes sure each
order is rewarded only once.

```
Game (WebGL)                     Worker (Cloudflare)              Midtrans
───────────                      ───────────────────              ────────
Buy ─────────── POST /orders ──► price from catalog.json ───────► create Snap transaction
Snap overlay ◄── token ─────────                                    │
player pays ────────────────────────────────────────────────────►   │
                                 POST /midtrans/notify ◄──────────  webhook (signed)
poll ────────── GET /orders/:id ► status: paid
claim ───────── POST /orders/:id/claim ► claimed (only once)
grant rewards (RewardSystem) + remember order on this device
```

Unfinished orders (e.g. bank transfer paid later) are resumed automatically
the next time the game runs.

## 1. Midtrans (start with Sandbox)

1. Create an account at https://dashboard.midtrans.com and switch to **Sandbox**.
2. **Settings → Access Keys**: copy the **Server Key** (secret) and **Client Key** (public).
3. **Settings → Payment → Notification URL**: set to
   `https://<your-worker>.workers.dev/midtrans/notify` (after step 2 below).

## 2. Cloudflare Worker

```bash
cd payments/worker
wrangler login
wrangler kv namespace create ORDERS            # paste the id into wrangler.toml → id
wrangler kv namespace create ORDERS --preview  # paste into → preview_id
wrangler secret put MIDTRANS_SERVER_KEY        # paste the Midtrans SERVER key
wrangler deploy                                # prints https://concept-wars-server.<account>.workers.dev
```

`wrangler.toml` vars:

| Var | Meaning |
|---|---|
| `MIDTRANS_PRODUCTION` | `"false"` = sandbox, `"true"` = real money |
| `ALLOWED_ORIGINS` | Origins allowed to call the API. `*.itch.zone` covers itch.io HTML5 games. |
| `FINISH_URL` | Optional page Midtrans returns to after a redirect checkout |

Check it: `curl https://<worker>/health` → `{"ok":true,"production":false}`.

## 3. Unity

On the **BundleSystem** object in `00_Boot`:

| Field | Value |
|---|---|
| Payment Server Url | the worker URL from `wrangler deploy` |
| Midtrans Client Key | the Midtrans **client** key (never the server key) |
| Midtrans Production | off for sandbox |
| Catalogs | GemsBundle, PackBundle (already set) |

With the URL empty, the Editor simulates purchases (free) and builds show
"Top-up is not available" unless **Allow Dummy Payments** is ticked.

**Changing bundles or prices:** every bundle needs a unique `productId`
(never rename one after release). Then run **Tools → Payments → Export Bundle
Catalog** and `wrangler deploy` again; the server only charges prices from
`src/catalog.json`.

## 4. Test in Sandbox

1. Build WebGL, open the top-up, buy a bundle; the Snap window opens in the page.
2. Pick QRIS / a bank VA and pay it with the Midtrans simulator:
   https://simulator.sandbox.midtrans.com
3. The game polls, claims and shows the rewards popup. Closing Snap after
   choosing a VA also works: rewards arrive once the simulator pays it.

## 5. Go live

1. Complete Midtrans business verification (KYC) and activate production.
2. `wrangler secret put MIDTRANS_SERVER_KEY` with the **production** server key,
   set `MIDTRANS_PRODUCTION = "true"`, `wrangler deploy`.
3. In Unity: production client key, tick **Midtrans Production**, rebuild.
4. Set the production Notification URL in the Midtrans dashboard.
5. Check itch.io's and Midtrans' terms for selling in-game currency.

## Known limits

- Saves are local (browser storage), so the server guarantees each payment
  is claimed once, but it can't stop someone editing their own save file.
- Rewards are tied to a random per-browser player ID: clearing site data
  before an unfinished order is claimed loses the link to that order (the
  payment is still visible in the Midtrans dashboard for manual support).
- Refunds/chargebacks are not handled automatically.

## 6. Email login and cloud saves

Players log in with their email: the server emails a 6-digit code, no
password. Their save files are then stored on the server, so progress
survives clearing browser data, new itch.io uploads and switching devices.
Guests can still play; their progress stays in that browser until they log in.

```
Game                                  Worker                         Resend
────                                  ──────                         ──────
email ──────── POST /auth/request ──► code (hashed in KV, 10 min) ──► email with the code
code ───────── POST /auth/verify ───► signed session token (30 days)
boot / saves ─ GET / PUT /save ─────► save:<account> in KV (revision-checked)
```

- Code: `worker/src/account.js` (server), `Game/13_Account/AccountSystem.cs`
  (client), `UI/Components/Account/` (login popup, Settings row).
- Boot: a logged-in player's newer cloud save is written to disk before the
  other systems load it. Every save uploads a few seconds later.
- Two devices: if both changed since the last sync, the game asks which
  progress to keep (cloud vs this device, with level / stages / chapters).
- Purchases made while logged in belong to the account.

### Setup (once)

1. **Resend** (sends the emails): create an account at https://resend.com.
   - Domains → Add Domain → `slafurrystudios.com`, then add the DNS records
     it shows (SPF/DKIM) at your domain registrar and wait until it's Verified.
   - API Keys → create a key with "Sending access".
2. **Worker secrets** (from `payments/worker`):
   ```bash
   wrangler secret put AUTH_SECRET      # any long random string, e.g. `openssl rand -base64 48`
   wrangler secret put RESEND_API_KEY   # the Resend key
   ```
   `MAIL_FROM` in `wrangler.toml` is `Concept Wars <login@slafurrystudios.com>`;
   any address on the verified domain works.
3. `wrangler deploy`
4. Unity: nothing to set. `AccountSystem` (in `00_Boot`) uses the same server
   URL as `BundleSystem` unless its own **Server Url** is filled in.

Check: `curl -X POST https://<worker>/auth/request -H 'Content-Type: application/json' -d '{"email":"you@example.com"}'`
should answer `{"ok":true,...}` and the email should arrive.

Keep `AUTH_SECRET` fixed: changing it logs everyone out (their saves stay).

### Limits

- Codes: one request per minute and 10 per day per email; 5 wrong tries
  burn a code. Sessions last 30 days.
- Saves are up to 1 MB and written by the game, so this protects players
  from losing progress, not from editing their own save.

### Testing locally

1. `payments/worker/.dev.vars` (gitignored):
   ```
   AUTH_SECRET="any-local-secret"
   DEV_RETURN_CODE="true"
   ```
   `DEV_RETURN_CODE` makes `/auth/request` return the code instead of
   emailing it, and the login popup fills it in. Never set it in production.
2. `wrangler dev --local --port 8787`
3. Point the Editor at it (Editor only, never in builds): run once in the
   Editor `EditorPrefs.SetString("ConceptWars.AccountServerOverride", "http://localhost:8787")`,
   and `EditorPrefs.DeleteKey(...)` the same key when done.

