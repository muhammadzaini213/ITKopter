// ITKopter top-up payment backend (Cloudflare Worker + KV + Midtrans Snap).
//
// Endpoints
//   POST /orders                 { productId, playerId }      -> { orderId, token, redirectUrl, amount }
//   GET  /orders/:id?playerId=   -> { orderId, productId, status }
//   POST /orders/:id/claim       { playerId }                 -> { orderId, productId, status: "claimed" }
//   POST /midtrans/notify        Midtrans HTTP notification (webhook)
//   GET  /health
//
// Order status: pending -> paid -> claimed   (or failed)
//
// Prices come from src/catalog.json (exported from Unity via
// Tools > Payments > Export Bundle Catalog), never from the client.
// The Midtrans server key is a Worker secret (MIDTRANS_SERVER_KEY).

import catalog from "./catalog.json";

const ORDER_TTL_SECONDS = 60 * 60 * 24 * 30; // keep orders 30 days

export default {
  async fetch(request, env) {
    const cors = corsHeaders(request, env);

    if (request.method === "OPTIONS") {
      return new Response(null, { status: 204, headers: cors });
    }

    try {
      const url = new URL(request.url);
      const path = url.pathname.replace(/\/+$/, "");
      let response;

      if (request.method === "GET" && path === "/health") {
        response = json({ ok: true, production: isProduction(env) });
      } else if (request.method === "POST" && path === "/orders") {
        response = await createOrder(request, env);
      } else if (request.method === "POST" && path === "/midtrans/notify") {
        response = await handleNotification(request, env);
      } else {
        const match = path.match(/^\/orders\/([A-Za-z0-9_-]{8,64})(\/claim)?$/);

        if (match && request.method === "GET" && !match[2]) {
          response = await getOrder(match[1], url.searchParams.get("playerId"), env);
        } else if (match && request.method === "POST" && match[2]) {
          response = await claimOrder(match[1], request, env);
        } else {
          response = json({ error: "not_found" }, 404);
        }
      }

      return withHeaders(response, cors);
    } catch (err) {
      console.error(err);
      return withHeaders(json({ error: "server_error" }, 500), cors);
    }
  },
};

// ---------------------------------------------------------------------------
// Orders
// ---------------------------------------------------------------------------

async function createOrder(request, env) {
  const body = await readJson(request);
  const productId = String(body.productId || "");
  const playerId = String(body.playerId || "");

  if (!isValidPlayerId(playerId)) {
    return json({ error: "invalid_player" }, 400);
  }

  const product = catalog.products.find((p) => p.productId === productId);

  if (!product) {
    return json({ error: "unknown_product" }, 400);
  }

  if (!env.MIDTRANS_SERVER_KEY) {
    return json({ error: "payments_not_configured" }, 503);
  }

  const orderId = "itk-" + crypto.randomUUID().replace(/-/g, "").slice(0, 24);
  const amount = Math.round(product.price);

  const snapBody = {
    transaction_details: { order_id: orderId, gross_amount: amount },
    item_details: [
      {
        id: product.productId,
        price: amount,
        quantity: 1,
        name: product.name.slice(0, 50),
      },
    ],
    custom_field1: playerId,
  };

  if (env.FINISH_URL) {
    snapBody.callbacks = { finish: env.FINISH_URL };
  }

  const snapResponse = await fetch(snapBaseUrl(env) + "/snap/v1/transactions", {
    method: "POST",
    headers: midtransHeaders(env),
    body: JSON.stringify(snapBody),
  });

  const snap = await snapResponse.json().catch(() => ({}));

  if (!snapResponse.ok || !snap.token) {
    console.error("Snap error", snapResponse.status, JSON.stringify(snap));
    return json({ error: "gateway_error", detail: snap.error_messages || null }, 502);
  }

  const order = {
    orderId,
    productId,
    playerId,
    amount,
    status: "pending",
    createdAt: Date.now(),
  };

  await putOrder(env, order);

  return json({
    orderId,
    productId,
    amount,
    token: snap.token,
    redirectUrl: snap.redirect_url,
  });
}

async function getOrder(orderId, playerId, env) {
  const order = await loadOrder(env, orderId);

  if (!order || order.playerId !== playerId) {
    return json({ error: "not_found" }, 404);
  }

  // Webhook not received yet (or not configured): ask Midtrans directly.
  if (order.status === "pending") {
    await refreshFromMidtrans(env, order);
  }

  return json(publicOrder(order));
}

async function claimOrder(orderId, request, env) {
  const body = await readJson(request);
  const order = await loadOrder(env, orderId);

  if (!order || order.playerId !== String(body.playerId || "")) {
    return json({ error: "not_found" }, 404);
  }

  if (order.status === "pending") {
    await refreshFromMidtrans(env, order);
  }

  if (order.status === "claimed") {
    // Already granted once: never grant again.
    return json({ ...publicOrder(order), alreadyClaimed: true }, 409);
  }

  if (order.status !== "paid") {
    return json({ ...publicOrder(order), error: "not_paid" }, 409);
  }

  order.status = "claimed";
  order.claimedAt = Date.now();
  await putOrder(env, order);

  return json(publicOrder(order));
}

// ---------------------------------------------------------------------------
// Midtrans
// ---------------------------------------------------------------------------

async function handleNotification(request, env) {
  const note = await readJson(request);
  const orderId = String(note.order_id || "");

  if (!orderId || !env.MIDTRANS_SERVER_KEY) {
    return json({ error: "bad_request" }, 400);
  }

  // signature_key = SHA512(order_id + status_code + gross_amount + ServerKey)
  const expected = await sha512Hex(
    orderId + String(note.status_code || "") + String(note.gross_amount || "") + env.MIDTRANS_SERVER_KEY
  );

  if (!timingSafeEqual(expected, String(note.signature_key || ""))) {
    console.warn("Bad Midtrans signature for", orderId);
    return json({ error: "invalid_signature" }, 401);
  }

  const order = await loadOrder(env, orderId);

  if (!order) {
    // Unknown to us (e.g. a dashboard test notification): acknowledge.
    return json({ ok: true, ignored: true });
  }

  applyMidtransStatus(order, note);
  await putOrder(env, order);

  return json({ ok: true });
}

async function refreshFromMidtrans(env, order) {
  if (!env.MIDTRANS_SERVER_KEY) return;

  const response = await fetch(apiBaseUrl(env) + "/v2/" + encodeURIComponent(order.orderId) + "/status", {
    headers: midtransHeaders(env),
  });

  if (!response.ok) return;

  const status = await response.json().catch(() => null);

  // 404 inside the body = the player hasn't picked a payment method yet.
  if (!status || String(status.status_code) === "404") return;

  const before = order.status;
  applyMidtransStatus(order, status);

  if (order.status !== before) {
    await putOrder(env, order);
  }
}

function applyMidtransStatus(order, data) {
  if (order.status === "claimed") return; // final

  // The paid amount must match what we charged.
  const gross = Math.round(Number(data.gross_amount));
  if (Number.isFinite(gross) && gross !== order.amount) {
    console.warn("Amount mismatch on", order.orderId, gross, order.amount);
    order.status = "failed";
    order.failReason = "amount_mismatch";
    return;
  }

  const ts = String(data.transaction_status || "");
  const fraud = String(data.fraud_status || "accept");

  if ((ts === "capture" && fraud === "accept") || ts === "settlement") {
    order.status = "paid";
    order.paidAt = order.paidAt || Date.now();
    order.paymentType = data.payment_type || null;
  } else if (["deny", "cancel", "expire", "failure"].includes(ts) || fraud === "deny") {
    order.status = "failed";
    order.failReason = ts || "denied";
  }
  // "pending" (e.g. waiting for VA transfer / QRIS scan) stays pending.
}

function isProduction(env) {
  return String(env.MIDTRANS_PRODUCTION || "false") === "true";
}

function snapBaseUrl(env) {
  return isProduction(env) ? "https://app.midtrans.com" : "https://app.sandbox.midtrans.com";
}

function apiBaseUrl(env) {
  return isProduction(env) ? "https://api.midtrans.com" : "https://api.sandbox.midtrans.com";
}

function midtransHeaders(env) {
  return {
    "Content-Type": "application/json",
    Accept: "application/json",
    Authorization: "Basic " + btoa(env.MIDTRANS_SERVER_KEY + ":"),
  };
}

// ---------------------------------------------------------------------------
// Storage + helpers
// ---------------------------------------------------------------------------

async function loadOrder(env, orderId) {
  const raw = await env.ORDERS.get("order:" + orderId);
  return raw ? JSON.parse(raw) : null;
}

async function putOrder(env, order) {
  await env.ORDERS.put("order:" + order.orderId, JSON.stringify(order), {
    expirationTtl: ORDER_TTL_SECONDS,
  });
}

function publicOrder(order) {
  return {
    orderId: order.orderId,
    productId: order.productId,
    amount: order.amount,
    status: order.status,
  };
}

function isValidPlayerId(id) {
  return /^[A-Za-z0-9_-]{8,64}$/.test(id);
}

async function readJson(request) {
  try {
    return await request.json();
  } catch {
    return {};
  }
}

async function sha512Hex(text) {
  const digest = await crypto.subtle.digest("SHA-512", new TextEncoder().encode(text));
  return [...new Uint8Array(digest)].map((b) => b.toString(16).padStart(2, "0")).join("");
}

function timingSafeEqual(a, b) {
  if (a.length !== b.length) return false;
  let diff = 0;
  for (let i = 0; i < a.length; i++) diff |= a.charCodeAt(i) ^ b.charCodeAt(i);
  return diff === 0;
}

function json(data, status = 200) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

function corsHeaders(request, env) {
  const origin = request.headers.get("Origin") || "";
  const allowed = String(env.ALLOWED_ORIGINS || "*")
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean);

  let allow = "";
  if (allowed.includes("*")) {
    allow = "*";
  } else if (
    allowed.some((rule) =>
      rule.startsWith("*.") ? origin.endsWith(rule.slice(1)) : origin === rule
    )
  ) {
    allow = origin;
  }

  const headers = {
    "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
    "Access-Control-Allow-Headers": "Content-Type",
    "Access-Control-Max-Age": "86400",
    Vary: "Origin",
  };

  if (allow) headers["Access-Control-Allow-Origin"] = allow;
  return headers;
}

function withHeaders(response, headers) {
  const out = new Response(response.body, response);
  for (const [k, v] of Object.entries(headers)) out.headers.set(k, v);
  return out;
}
