# GitHub Workflows

CI/CD lives in [`.github/workflows/`](./.github/workflows/). Four workflows are available:

| Workflow | File | Trigger | What it does |
|---|---|---|---|
| Unity Compile | `unity-compile.yml` | **Pull requests** (paths: `Assets/`, `Packages/`, `ProjectSettings/`) | Batchmode compile gate — fails the PR on `error CS` or non-successful Unity exit |
| Track Drive Changes | `track.yml` | Daily at 07:00 WITA (cron `0 23 * * *`) + manual | Scans Google Drive sprite/audio folders, updates `state/` manifests **on `main`** (`[skip ci]`), Discord-notifies new/changed/removed-once |
| Retrieve New Drive Files | `retrieve.yml` | Manual only | Downloads new Drive files into `Assets/_Game/02_Art/Sprite` and `Assets/_Game/03_Audio`; commits **state → `main`** immediately; assets → disposable `chore/asset` PR → `main` |
| Build and Deploy to itch.io | `unity-itchio-deploy.yml` | Manual only | Builds Windows/macOS/WebGL with Unity, pushes to itch.io via butler, optionally creates a GitHub Release, then posts a Discord announcement |

## Setup (repository secrets & variables)

All configuration is done under **Settings → Secrets and variables → Actions** on the GitHub repo. Nothing is hardcoded.

**Convention** (easy to spot in the workflow YAML):

- **Everything is declared once** in the workflow-level `env:` block — never hardcode values in `run` steps.
- **Variables** (non-credential) come from `vars.*`; **secrets** (credentials) come from `secrets.*`.
- Steps only reference env names as `"$NAME"` (no inline `${{ }}` in scripts).

**Secrets** (Settings → Secrets → Actions) — credentials only:

| Secret | Used by | Purpose |
|---|---|---|
| `GOOGLE_SERVICE_ACCOUNT_JSON_B64` | track, retrieve | Base64-encoded Google service-account JSON with access to the Drive folders |
| `GEMINI_API_KEY` | track, retrieve, deploy | Gemini API key for asset tagging / release flavor text |
| `DISCORD_WEBHOOK_URL` | track, retrieve | Webhook for asset-sync notifications (URL grants posting access) |
| `DISCORD_WEBHOOK_URL_DEPLOY` | deploy | Webhook for build/deploy announcements |
| `UNITY_EMAIL` | compile, deploy, retrieve | Unity account email (Personal license activation) |
| `UNITY_PASSWORD` | compile, deploy, retrieve | Unity account password |
| `BUTLER_API_KEY` | deploy | itch.io butler API key ([create here](https://itch.io/user/settings/api-keys)) |
| `PR_PAT` | retrieve | Optional. Personal access token for creating the asset PR; falls back to the default `GITHUB_TOKEN` |

**Variables** (Settings → Variables → Actions) — non-credential config:

| Variable | Used by | Default | Purpose |
|---|---|---|---|
| `DRIVE_SPRITE_FOLDER_ID` | track, retrieve | — | Google Drive folder ID for sprites (**required** for asset workflows) |
| `DRIVE_AUDIO_FOLDER_ID` | track, retrieve | — | Google Drive folder ID for audio (**required** for asset workflows) |
| `GEMINI_MODEL` | track, retrieve, deploy | `gemini-2.5-flash` | Gemini model ID |
| `GEMINI_PERSONA` | track, retrieve, deploy | deploy only (see workflow) | Persona prompt used when tagging assets / writing announcements |
| `GEMINI_LANGUAGE` | deploy | *(empty)* | Force the announcement language (e.g. `Indonesian`) |
| `BOT_GIT_USERNAME` / `BOT_GIT_EMAIL` | track, retrieve | `Kozeki Ui` / bot noreply | Commit author for bot commits |
| `ITCH_USER` | deploy | — | Your itch.io username (**required**) |
| `ITCH_GAME` | deploy | — | Your itch.io game URL slug (**required**) |
| `GAME_DISPLAY_NAME` | deploy | `ITCH_GAME` | Human-readable title used in Discord posts |
| `ITCH_CHANNEL_WINDOWS` / `ITCH_CHANNEL_MAC` / `ITCH_CHANNEL_WEBGL` | deploy | `windows` / `mac` / `html5` | itch.io channel names per platform |
| `UNITY_PROJECT_PATH` | deploy, retrieve | `.` | Path to the Unity project if it's not the repo root |

## Running the workflows

1. Go to the repo on GitHub → **Actions** tab.
2. Select the workflow in the left sidebar.
3. Click **Run workflow**, pick the branch (`main`) and (for deploy) choose inputs:
   - **platforms**: `All`, `Windows`, `macOS`, or `WebGL`
   - **release_type**: `release`, `beta`, or `alpha` — appended to the version tag, e.g. `v1.2.3-release`, `v1.2.3-beta`, `v1.2.3-alpha`
   - **release_tag**: optional version number, e.g. `1.2.3` — the `v` prefix and `-{release_type}` suffix are added automatically, so just type `X.X.X`. Leave empty for an unversioned build (no GitHub Release). The last published version is shown as context in the run log
4. Click **Run workflow** and watch the run log.

`track.yml` also runs on its schedule; `retrieve.yml` and `unity-itchio-deploy.yml` are manual-only. `unity-compile.yml` runs automatically on PRs that touch game/project files.

## Notes

- **PR compile gate**: require the `Batchmode compile` check in branch protection (**Settings → Branches**) so broken C# cannot merge. Docs-only PRs skip the workflow via path filters.
- **Failure Discord**: deploy / track / retrieve each have a `notify-failure` job (`if: failure()`) that posts a Gemini-written embed to the same webhook as success (`DISCORD_WEBHOOK_URL_DEPLOY` for deploy, `DISCORD_WEBHOOK_URL` for asset bots) — matches Potkeeter.
- **Asset flow**: `track` detects changes and **persists `state/` to `main`** (so `deleted_in_drive` / retrieve timestamps stick — otherwise Discord re-notifies the same deletes every day). `retrieve` downloads files, also **persists `state/` to `main`**, then opens a PR for **assets only** (`chore/asset` → `main`). Review and merge the asset PR to accept files. The `chore/asset` branch is recreated from `main` on every run — never push to it manually.
- **`.meta` generation**: after downloading, `retrieve` runs a Unity batchmode import so every new Drive file gets its generated `.meta` (stable GUID + import settings). Those `.meta` files are committed in the asset PR. Requires `UNITY_EMAIL`/`UNITY_PASSWORD` (same secrets as compile/deploy).
- Bot commits include `[skip ci]` so they don't trigger other workflows.
- **Deploy requirements**: the project must have `Assets/Editor/BuildScript.cs` with a `BuildScript.Build` method (already present) and a valid Unity Personal license (`UNITY_EMAIL`/`UNITY_PASSWORD`). No Git LFS — every checkout action is plain `actions/checkout@v4` and binaries are regular blobs.
- Build artifacts are kept for 7 days on the workflow run; itch.io is the long-term host.
- First deploy: make sure the itch.io game exists and `ITCH_USER`/`ITCH_GAME` match its URL (`https://<ITCH_USER>.itch.io/<ITCH_GAME>`).
- **Discord bot context**: before notifying, the deploy workflow builds a changelog (`git log` since the previous release tag, or previous commit → `HEAD` if no tags) and passes it to Gemini so the announcement can highlight what changed. The GitHub Release notes (when `release_tag` is set) are also written by Gemini from the same commit changelog.
- **Deploy speedups**: the Unity editor install is cached between runs (`cache-installation: true`) and the Unity Hub auto-update is disabled (`auto-update-hub: false`) so repeated runs skip the big editor download.
