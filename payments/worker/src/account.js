// Email-code login and cloud saves for Concept Wars.
//
// Endpoints
//   POST /auth/request   { email }              -> { ok, expiresIn }        emails a 6-digit code
//   POST /auth/verify    { email, code }        -> { token, accountId, email, expiresAt }
//   GET  /auth/me        (Bearer token)         -> { accountId, email }
//   GET  /save           (Bearer token)         -> { revision, savedAt, summary, data } | 404
//   PUT  /save           (Bearer token)
//        { data, summary, savedAt, baseRevision, force? } -> { revision, savedAt }
//        409 { error: "conflict", revision, savedAt, summary } when the cloud
//        save changed since baseRevision (another device) and force is not set.
//
// Storage (KV, same namespace as orders, separate key prefixes):
//   code:<email>        { hash, attempts }            TTL 10 min
//   rl:<email>          request throttle               TTL 60 s
//   rld:<email>         requests today (count)         TTL 24 h
//   acct-email:<email>  accountId
//   acct:<id>           { id, email, createdAt }
//   save:<id>           { revision, savedAt, summary, data }
//
// Secrets: AUTH_SECRET (signs sessions + hashes codes), RESEND_API_KEY (email).
// Vars: MAIL_FROM ("Concept Wars <login@slafurrystudios.com>").
// Local testing only: DEV_RETURN_CODE = "true" returns the code in the
// /auth/request response instead of emailing it (never set in production).

const CODE_TTL_SECONDS = 10 * 60;
const MAX_CODE_ATTEMPTS = 5;
const REQUEST_THROTTLE_SECONDS = 60;
const MAX_REQUESTS_PER_DAY = 10;
const SESSION_DAYS = 30;
const MAX_SAVE_BYTES = 1024 * 1024; // 1 MB

/** Handles /auth/* and /save. Returns null for any other path. */
export async function handleAccount(request, env, path) {
  const method = request.method;

  if (path === "/auth/request" && method === "POST") return requestCode(request, env);
  if (path === "/auth/verify" && method === "POST") return verifyCode(request, env);
  if (path === "/auth/me" && method === "GET") return withSession(request, env, (s) => json({ accountId: s.sub, email: s.email }));
  if (path === "/save" && method === "GET") return withSession(request, env, (s) => getSave(env, s));
  if (path === "/save" && method === "PUT") return withSession(request, env, (s) => putSave(request, env, s));

  return null;
}

// ---------------------------------------------------------------------------
// Login
// ---------------------------------------------------------------------------

async function requestCode(request, env) {
  if (!env.AUTH_SECRET) return json({ error: "auth_not_configured" }, 503);

  const body = await readJson(request);
  const email = normalizeEmail(body.email);
  if (!email) return json({ error: "invalid_email" }, 400);

  if (await env.ORDERS.get("rl:" + email)) {
    return json({ error: "too_soon", retryIn: REQUEST_THROTTLE_SECONDS }, 429);
  }

  const today = parseInt((await env.ORDERS.get("rld:" + email)) || "0", 10);
  if (today >= MAX_REQUESTS_PER_DAY) return json({ error: "too_many_requests" }, 429);

  const code = randomCode();
  await env.ORDERS.put("code:" + email, JSON.stringify({ hash: await codeHash(env, email, code), attempts: 0 }), {
    expirationTtl: CODE_TTL_SECONDS,
  });
  await env.ORDERS.put("rl:" + email, "1", { expirationTtl: REQUEST_THROTTLE_SECONDS });
  await env.ORDERS.put("rld:" + email, String(today + 1), { expirationTtl: 24 * 60 * 60 });

  if (env.DEV_RETURN_CODE === "true") {
    console.log(`[dev] login code for ${email}: ${code}`);
    return json({ ok: true, expiresIn: CODE_TTL_SECONDS, devCode: code });
  }

  const sent = await sendCodeEmail(env, email, code);
  if (!sent) return json({ error: "email_failed" }, 502);

  return json({ ok: true, expiresIn: CODE_TTL_SECONDS });
}

async function verifyCode(request, env) {
  if (!env.AUTH_SECRET) return json({ error: "auth_not_configured" }, 503);

  const body = await readJson(request);
  const email = normalizeEmail(body.email);
  const code = String(body.code || "").replace(/\D/g, "");
  if (!email || code.length !== 6) return json({ error: "invalid_code" }, 400);

  const key = "code:" + email;
  const raw = await env.ORDERS.get(key);
  if (!raw) return json({ error: "code_expired" }, 400);

  const record = JSON.parse(raw);

  if (!timingSafeEqual(record.hash, await codeHash(env, email, code))) {
    record.attempts = (record.attempts || 0) + 1;

    if (record.attempts >= MAX_CODE_ATTEMPTS) {
      await env.ORDERS.delete(key);
      return json({ error: "code_expired" }, 400);
    }

    await env.ORDERS.put(key, JSON.stringify(record), { expirationTtl: CODE_TTL_SECONDS });
    return json({ error: "wrong_code", attemptsLeft: MAX_CODE_ATTEMPTS - record.attempts }, 400);
  }

  await env.ORDERS.delete(key);

  let accountId = await env.ORDERS.get("acct-email:" + email);
  if (!accountId) {
    accountId = "acc_" + crypto.randomUUID().replace(/-/g, "").slice(0, 24);
    await env.ORDERS.put("acct:" + accountId, JSON.stringify({ id: accountId, email, createdAt: Date.now() }));
    await env.ORDERS.put("acct-email:" + email, accountId);
  }

  const expiresAt = Date.now() + SESSION_DAYS * 24 * 60 * 60 * 1000;
  const token = await signToken(env, { sub: accountId, email, exp: expiresAt });

  return json({ token, accountId, email, expiresAt });
}

async function sendCodeEmail(env, email, code) {
  if (!env.RESEND_API_KEY || !env.MAIL_FROM) {
    console.error("Email not configured (RESEND_API_KEY / MAIL_FROM).");
    return false;
  }

  const text =
    `Your Concept Wars login code is ${code}\n\n` +
    `It expires in 10 minutes. If you didn't try to log in, you can ignore this email.`;

  const html =
    `<div style="font-family:Arial,sans-serif;background:#071724;color:#e6f7ff;padding:24px">` +
    `<p style="margin:0 0 12px">Your <b>Concept Wars</b> login code:</p>` +
    `<p style="font-size:32px;letter-spacing:8px;color:#19f0ff;margin:0 0 16px"><b>${code}</b></p>` +
    `<p style="margin:0;color:#8fa9b5">It expires in 10 minutes. If you didn't try to log in, ignore this email.</p></div>`;

  const response = await fetch("https://api.resend.com/emails", {
    method: "POST",
    headers: { Authorization: "Bearer " + env.RESEND_API_KEY, "Content-Type": "application/json" },
    body: JSON.stringify({ from: env.MAIL_FROM, to: [email], subject: `Concept Wars login code: ${code}`, text, html }),
  });

  if (!response.ok) {
    console.error("Resend error", response.status, await response.text().catch(() => ""));
    return false;
  }

  return true;
}

// ---------------------------------------------------------------------------
// Cloud save
// ---------------------------------------------------------------------------

async function getSave(env, session) {
  const raw = await env.ORDERS.get("save:" + session.sub);
  if (!raw) return json({ error: "no_save" }, 404);
  return new Response(raw, { headers: { "Content-Type": "application/json" } });
}

async function putSave(request, env, session) {
  const text = await request.text();
  if (text.length > MAX_SAVE_BYTES) return json({ error: "save_too_large" }, 413);

  let body;
  try {
    body = JSON.parse(text);
  } catch {
    return json({ error: "bad_request" }, 400);
  }

  if (!body || typeof body.data !== "object" || body.data === null) return json({ error: "bad_request" }, 400);

  const key = "save:" + session.sub;
  const currentRaw = await env.ORDERS.get(key);
  const current = currentRaw ? JSON.parse(currentRaw) : null;
  const currentRevision = current ? current.revision : 0;

  // Someone else (another device) saved since this client last synced.
  if (!body.force && Number(body.baseRevision || 0) !== currentRevision) {
    return json({ error: "conflict", revision: currentRevision, savedAt: current?.savedAt ?? 0, summary: current?.summary ?? null }, 409);
  }

  const record = {
    revision: currentRevision + 1,
    savedAt: Date.now(),
    clientSavedAt: Number(body.savedAt || 0),
    summary: typeof body.summary === "object" ? body.summary : null,
    data: body.data,
  };

  await env.ORDERS.put(key, JSON.stringify(record));
  return json({ revision: record.revision, savedAt: record.savedAt });
}

// ---------------------------------------------------------------------------
// Sessions (HMAC-signed tokens)
// ---------------------------------------------------------------------------

async function withSession(request, env, handler) {
  const header = request.headers.get("Authorization") || "";
  const token = header.startsWith("Bearer ") ? header.slice(7) : "";
  const session = token ? await verifyToken(env, token) : null;

  if (!session) return json({ error: "unauthorized" }, 401);
  return handler(session);
}

async function signToken(env, payload) {
  const body = base64url(new TextEncoder().encode(JSON.stringify(payload)));
  const sig = base64url(await hmac(env.AUTH_SECRET, body));
  return body + "." + sig;
}

async function verifyToken(env, token) {
  if (!env.AUTH_SECRET) return null;

  const [body, sig] = token.split(".");
  if (!body || !sig) return null;

  const expected = base64url(await hmac(env.AUTH_SECRET, body));
  if (!timingSafeEqual(expected, sig)) return null;

  try {
    const payload = JSON.parse(new TextDecoder().decode(fromBase64url(body)));
    if (!payload.sub || !payload.exp || payload.exp < Date.now()) return null;
    return payload;
  } catch {
    return null;
  }
}

async function hmac(secret, text) {
  const key = await crypto.subtle.importKey("raw", new TextEncoder().encode(secret), { name: "HMAC", hash: "SHA-256" }, false, ["sign"]);
  return new Uint8Array(await crypto.subtle.sign("HMAC", key, new TextEncoder().encode(text)));
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function normalizeEmail(value) {
  const email = String(value || "").trim().toLowerCase();
  return email.length <= 254 && /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(email) ? email : null;
}

function randomCode() {
  const n = crypto.getRandomValues(new Uint32Array(1))[0] % 1000000;
  return String(n).padStart(6, "0");
}

async function codeHash(env, email, code) {
  const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(`${code}:${email}:${env.AUTH_SECRET}`));
  return [...new Uint8Array(digest)].map((b) => b.toString(16).padStart(2, "0")).join("");
}

function base64url(bytes) {
  let s = "";
  for (const b of bytes) s += String.fromCharCode(b);
  return btoa(s).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

function fromBase64url(text) {
  const s = atob(text.replace(/-/g, "+").replace(/_/g, "/") + "===".slice((text.length + 3) % 4));
  return Uint8Array.from(s, (c) => c.charCodeAt(0));
}

function timingSafeEqual(a, b) {
  a = String(a);
  b = String(b);
  if (a.length !== b.length) return false;
  let diff = 0;
  for (let i = 0; i < a.length; i++) diff |= a.charCodeAt(i) ^ b.charCodeAt(i);
  return diff === 0;
}

async function readJson(request) {
  try {
    return await request.json();
  } catch {
    return {};
  }
}

function json(data, status = 200) {
  return new Response(JSON.stringify(data), { status, headers: { "Content-Type": "application/json" } });
}
