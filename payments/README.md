# Top-up payments (Midtrans + Cloudflare Workers)

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
wrangler deploy                                # prints https://itkopter-payments.<account>.workers.dev
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
