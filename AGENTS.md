# AGENTS.md

Unity **6000.3.25f1** (Unity 6.3 LTS) URP starter (C#). Verify C# with Unity batchmode (see **Verify changes** — no wrapper script, no git hooks). Style: root **`.editorconfig`** (hints only). CI: GitHub Actions under `.github/workflows/` (PR compile gate `unity-compile` + itch.io deploy + Drive retrieve/track).

## Sources of truth

- **`ARCHITECTURE.md`** — folder rules, base classes, lifecycle, naming (System vs Manager vs Controller). Prefer this over README.
- **`README.md`** — pitch + setup; trees use `_Game/` + `Systems/`; only scene is `Boot.unity`.
- **`GITHUB_WORKFLOWS.md`** — secrets/vars setup, run steps, deploy inputs (`platforms`/`release_type`/`release_tag`), Discord + GitHub Release changelog.
- Executable wins over docs: `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, `.gitattributes`, `.github/workflows/`.

## Layout

```
Assets/_Game/          # all game content
  00_Scripts/          # Core, Systems, Manager, Game, UI, Utils
  01_Objects/          # Prefabs/ + Data/ (SO variants)
  02_Art/Sprite/       # Drive-synced sprites — retrieve.yml wipes + restores this
  03_Audio/            # GameAudioMixer.mixer (+ Music/, SFX/ for Drive clips)
  04_Scenes/           # Boot only (in Build Settings)
  05_Settings/         # URP, Input
Assets/Editor/         # BuildScript.cs (CI -executeMethod) + GameAssetCreator
Assets/_Vendor/        # third-party — do not edit
.github/workflows/     # unity-compile (PR), unity-itchio-deploy, retrieve, track
scripts/test_state.py       # Drive state regressions
retrieve.py, track.py, core/, state/, requirements.txt  # Drive → assets tooling
```

- Namespaces mirror folders: `Slafurry.Systems.*`, `Slafurry.Core.*`, `Slafurry.Utils.*`. Folder is **`Systems/`** (plural) — not `System/` (BCL conflict).
- **No `.asmdef`** — every non-`Editor` script compiles into `Assembly-CSharp`. Editor-only code must live under an `Editor/` folder.
- Internal rule: max 6 subfolders per folder (ARCHITECTURE.md).

## Boot / lifecycle (easy to get wrong)

- `Singleton`/`Manager` `Awake` is private and auto-calls `LoadingSystem.Instance.Register(this)`. **`LoadingSystem` must exist first** — in the only scene (`Assets/_Game/04_Scenes/Boot.unity`) it sits on its own **`====== LOADING ======`** root GameObject (script GUID `2ba4b0833086a19a0b6ff4653a00344a`); do not remove it. New systems attach to the **`====== SYSTEM ======`** root (or their own root), alongside `SceneLoader`, `SaveSystem`, `InputHub`, `PauseSystem`, `LocalizationSystem`.
- `Initialize()` = internal setup only (no other-object refs). Cross-object wiring only in `PostInitialize()`. Avoid `Start()` (guard with `_isReady` if unavoidable).
- `IInitializable.Priority` orders boot (lower first). Late registrants after boot get a one-frame delayed batch (`LoadingSystem`).
- Base class: cross-scene singleton → `GameSystem<T>`; scene-bound singleton → `LocalSingleton<T>`; session coordinator → `Manager` (registers with `GameManager` in `PostInitialize` via abstract `RegisterToGameManager`/`OnPostInitialize`).
- **`GameManager` does not exist** — `Manager.cs` and ARCHITECTURE still describe it; **any `Manager` subclass will not compile** until you add one (or drop the abstract hooks). No subclasses exist today.
- `BootstrapLoader` (Systems/Scene) is optional: wire `systemsToWaitFor` + `targetSceneName` if you add a boot scene. Default `targetSceneName` is `"MainMenu"` (not in Build Settings).

## Static facades (prefer over `.Instance` chains)

`Audio` · `Save` · `Pause` · `Controls` (input) · `Localize` · `SceneSystem`

- Pause is a **key stack** (`Pause.On("UI")` / `Pause.Off("UI")`), not a bool — multiple holders can keep pause.
- Save: Newtonsoft.Json → `Application.persistentDataPath/{fileName}.json` (caller omits `.json`).
- Linear volume → dB via `Mathf.Log10(x) * 20` before `AudioMixer.SetFloat`.
- `GameFeel` is a **MonoBehaviour**, not a static facade.

## GitHub Actions — config lives in workflow-level `env:`

All four workflows declare config in a workflow-level `env:` block; `run:` steps read `"$NAME"` only, never hardcoded values.

```yaml
env:
  # Non-credential (Settings → Variables)
  DRIVE_SPRITE_FOLDER_ID: ${{ vars.DRIVE_SPRITE_FOLDER_ID }}
  GEMINI_PERSONA: ${{ vars.GEMINI_PERSONA }}
  ...
  # Credentials (Settings → Secrets)
  GEMINI_API_KEY: ${{ secrets.GEMINI_API_KEY }}
  GOOGLE_SERVICE_ACCOUNT_JSON_B64: ${{ secrets.GOOGLE_SERVICE_ACCOUNT_JSON_B64 }}
  DISCORD_WEBHOOK_URL: ${{ secrets.DISCORD_WEBHOOK_URL }}
  GH_TOKEN: ${{ secrets.PR_PAT || secrets.GITHUB_TOKEN }}  # retrieve only
  UNITY_EMAIL / UNITY_PASSWORD                            # retrieve uses them too (meta import)
```

`secrets.*` still appears inline in **action `with:` inputs** (Unity license activation, Butler upload) and in **failure-notify step `env:`** blocks — that is intentional, not drift.

**Secrets** (credentials only): `GEMINI_API_KEY`, `GOOGLE_SERVICE_ACCOUNT_JSON_B64`, `DISCORD_WEBHOOK_URL`, `DISCORD_WEBHOOK_URL_DEPLOY`, `UNITY_EMAIL`, `UNITY_PASSWORD`, `BUTLER_API_KEY`, `PR_PAT` (**retrieve only** — compile/deploy use the default `GITHUB_TOKEN`).

**Variables** (non-credential): `DRIVE_SPRITE_FOLDER_ID`, `DRIVE_AUDIO_FOLDER_ID`, `GEMINI_MODEL`, `GEMINI_PERSONA`, `GEMINI_LANGUAGE`, `BOT_GIT_USERNAME`, `BOT_GIT_EMAIL`, `ITCH_*`, `GAME_DISPLAY_NAME`, `UNITY_PROJECT_PATH` (deploy, retrieve, compile).

**Retrieve runs a Unity batchmode import after downloading** so new Drive files get generated `.meta` (stable GUID + import settings) — committed inside the asset PR. Requires `UNITY_EMAIL`/`UNITY_PASSWORD` on `retrieve` now.

**Deploy inputs** (`unity-itchio-deploy.yml`, manual): `platforms` (All/Windows/macOS/WebGL) · `release_type` (release/beta/alpha) · `release_tag` (type `1.2.3`; the `v` prefix and `-{release_type}` suffix are added **automatically** → `v1.2.3-release` / `v1.2.3-beta` / `v1.2.3-alpha`). Leave `release_tag` empty for an unversioned build (no GitHub Release). The last published version is fetched via `gh api releases/latest` and shown in the log + Discord.

**Deploy changelogs** — two steps build a `git log` changelog (since previous tag, else `HEAD~1..HEAD`): the Discord notification uses it for Gemini **flavor text**; the GitHub Release body is written by **Gemini** from the same commit list. No `generate_release_notes`.

## Unity / git gotchas

- **No Git LFS.** All binaries are **plain git blobs**; `.gitattributes` marks them `binary` (`-diff -merge -text`). Do not reintroduce `filter=lfs` or `lfs: true` in workflow checkouts. Working tree may show dirty `_Vendor` files — **do not commit vendor churn**.
- `.gitattributes` sets `merge=unityyamlmerge` for `*.unity`/`*.asset`/etc., but the **driver is not in git config** — Unity YAML conflict merges fail until UnityYAMLMerge is configured.
- Serialization: Force Text YAML + Visible Meta Files. **Commit `.meta` with every new asset**; never hand-edit GUIDs.
- Input System only (`activeInputHandler: 2`). Actions: `Assets/_Game/05_Settings/Input/Main Input.inputactions` → generated `Main Input.cs` (regenerate in Unity after editing actions). Legacy `Input.GetKeyDown` in `DIalogHUD` is dead code.
- Editor menu: **Slafurry → Game Data** (GameAssetCreator). Types for that window use `[GameAssetCreator(category, displayName, order)]`; other SOs use `[CreateAssetMenu]`.
- `EditorBuildSettings` lists only `Boot` (enabled). Do not assume other scenes are loadable.
- `*.csproj` / `*.sln` are Editor-generated (`Assembly-CSharp.csproj` is gitignored) — never hand-edit or commit them.
- **Drive bots re-sync from `origin/main` every run**: `retrieve.yml` `rm -rf`s `state/`, `Assets/_Game/02_Art/Sprite`, `Assets/_Game/03_Audio` then restores via `git archive origin/main`. Commit `GameAudioMixer.mixer` (and local state) to `main` before relying on those workflows.
- **`track` / `retrieve` commit `state/` to `main`** (`[skip ci]`) so `deleted_in_drive` / `retrieve_status` / `downloaded_*` persist. Assets still go `chore/asset` → PR. `core/state.py` must keep retrieve fields when track merges a fresh listing (`scripts/test_state.py`).
- **No git hooks.** `.githooks/pre-push` and `scripts/check-compile.sh` were removed deliberately — do not recreate them; local verification is the raw batchmode command below.

## Verify changes

- **Style only:** `.editorconfig`.
- **Compile:** run Unity batchmode directly. Pass = exit 0, log contains `Exiting batchmode successfully`, and no `error CS` lines. (Editor path is not in the repo — read it from `ProjectSettings/ProjectVersion.txt` / your Unity Hub install.)
- **CI:** `unity-compile.yml` runs the same batchmode compile + `error CS` grep on **PRs only** (paths: `Assets/`, `Packages/`, `ProjectSettings/`, the workflow file). Require in branch protection. CI installs the editor from `ProjectVersion.txt` — no version pin there.
- **Drive tooling:** `pip install -r requirements.txt`, then `python3 scripts/test_state.py` after touching `core/state.py` / `track.py` / `retrieve.py` (state tests are dependency-free and pass as-is).

```bash
~/Unity/Hub/Editor/6000.3.25f1/Editor/Unity \
  -batchmode -nographics -quit \
  -projectPath "$PWD" -logFile /tmp/unity-compile.log
grep -n "error CS" /tmp/unity-compile.log
grep -q "Exiting batchmode successfully" /tmp/unity-compile.log && echo OK
```

Prefer this (or opening the Editor) after any C# change. Play-mode behavior still needs a manual Editor run (`Boot`).
