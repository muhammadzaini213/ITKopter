# AGENTS.md

Unity **6000.3.25f1** (Unity 6.3 LTS) URP starter (C#). Verify C# with Unity batchmode (see **Verify changes** — no wrapper script, no git hooks, **no CI compile gate** — `unity-compile.yml` was deleted on purpose). Style: root **`.editorconfig`** (hints only). CI: GitHub Actions under `.github/workflows/` — **itch.io deploy only**; Drive retrieve/track workflows + tooling removed.

## Sources of truth

- **`ARCHITECTURE.md`** — folder rules, base classes, lifecycle, naming (System vs Manager vs Controller). Prefer this over README.
- **`README.md`** — pitch + setup; trees use `_Game/` + `Systems/`.
- **`GITHUB_WORKFLOWS.md`** — secrets/vars setup, run steps, deploy inputs (`platforms`/`release_type`/`release_tag`), Discord + GitHub Release changelog.
- Executable wins over docs: `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, `.gitattributes`, `.github/workflows/`, `ProjectSettings/EditorBuildSettings.asset`.

## Layout

```
Assets/_Game/          # all game content
  00_Scripts/          # Core, Systems, Manager, Game, UI, Utils
  01_Objects/          # Prefabs/ + Data/ (SO variants; SceneReference assets in Data/00_Scenes/)
  02_Art/              # sprites (Drive sync removed)
  03_Audio/            # GameAudioMixer.mixer (+ Music/, SFX/)
  04_Scenes/           # 00_Boot … 11_StoryScene (numbered; see Build Settings below)
  05_Settings/         # URP, Input
Assets/Editor/         # BuildScript.cs (CI -executeMethod)
Assets/_Vendor/        # third-party — do not edit
.github/workflows/     # unity-itchio-deploy only
core/, state/, scripts/test_state.py   # orphaned Drive-tooling remnants (retrieve.py/track.py/requirements.txt removed) — do not extend
```

- Namespaces mirror folders: `Slafurry.Systems.*`, `Slafurry.Core.*`, `Slafurry.Utils.*`. Folder is **`Systems/`** (plural) — not `System/` (BCL conflict). **Exception:** active code under `00_Scripts/Game/` is in the **global namespace** (no `namespace` block) — match files there, don't add one. Only `_old/` files declare namespaces (`Game.Dialog`, …). Most `UI/` files also global; `Systems/Scene/SceneReference.cs` is global despite its folder.
- `00_Scripts/Game/` uses **numbered domain subfolders** (`00_Player`, `01_Currency`, `02_Characters`, `03_Gameplay`, `04_Gacha`, `05_Items`, `06_Bundle`, `07_Level`, `08_Reward`, `09_Shop`, `10_Inventory`, `11_Story`, `_old`). `03_Gameplay/` holds `PlayerMovement`, `PlayerAutoShooter`, projectiles. Gameplay `GameSystem<T>` subclasses (`PlayerSystem`, `CurrencySystem`, `EnergySystem`, `GachaSystem`, `RewardSystem`, `ShopSystem`, `BundleSystem`, `InventorySystem`) live **here**, not in `Systems/` — this deviates from ARCHITECTURE's "`GameSystem<T>` → `Systems/`" rule; follow the code, not the doc.
- **No `.asmdef`** — every non-`Editor` script compiles into `Assembly-CSharp`. Editor-only code must live under an `Editor/` folder.
- Internal rule: max 6 subfolders per folder (ARCHITECTURE.md).

## Boot / lifecycle (easy to get wrong)

- Scenes are numbered `00_Boot` … `11_StoryScene`. **`EditorBuildSettings` lists 10 enabled** (`00_Boot`–`09_SettingsMenu`); `10_StoryMenu` and `11_StoryScene` exist on disk but are **not** in Build Settings. Do not assume every scene is loadable.
- `Singleton`/`Manager` `Awake` is private and auto-calls `LoadingSystem.Instance.Register(this)`. **`LoadingSystem` must exist first** — in `Assets/_Game/04_Scenes/00_Boot.unity` it sits on its own root GameObject (script GUID `2ba4b0833086a19a0b6ff4653a00344a`); do not remove it.
- `00_Boot` root objects today (verified in scene YAML): `LoadingSystem`, `LocalizationSystem`, `AudioSystem` (children `MusicPlayer`/`SFXPlayer`), `SaveSystem`, `SceneLoader`, `PauseSystem`, gameplay `GameSystem<T>` roots — `PlayerSystem`, `CurrencySystem`, `EnergySystem`, `GachaSystem`, `RewardSystem`, `ShopSystem`, `BundleSystem`, `InventorySystem`, **each its own root GameObject** (no parent object named `GameSystem`), `====== BOOT ======` (BootstrapLoader), `====== SETUP ======` (EventSystem + light). **`InputHub` and `VFXSystem` are not in `00_Boot`** — add them if gameplay needs them. New systems attach as their own root GameObject.
- `Initialize()` = internal setup only (no other-object refs). Cross-object wiring only in `PostInitialize()`. Avoid `Start()` (guard with `_isReady` if unavoidable).
- `IInitializable.Priority` orders boot (lower first). Late registrants after boot get a one-frame delayed batch (`LoadingSystem.ProcessLateBatch`).
- Base class: cross-scene singleton → `GameSystem<T>`; scene-bound singleton → `LocalSingleton<T>`; session coordinator → `Manager` (registers with `GameManager` in `PostInitialize` via abstract `RegisterToGameManager`/`OnPostInitialize`).
- **`GameManager` does not exist** — `Manager.cs` and ARCHITECTURE still describe it; **any `Manager` subclass will not compile** until you add one (or drop the abstract hooks). No subclasses exist today (`ObjectiveManager` extends `Singleton<T>`, not `Manager`).
- `BootstrapLoader` (Systems/Scene, namespace `Slafurry.Systems.Bootstrap`) sits on `====== BOOT ======` in `00_Boot`. Its `targetSceneName` is a **`SceneReference` ScriptableObject** (asset under `01_Objects/Data/00_Scenes/`), currently `01_MainMenuScene` — not a raw string.

## Static facades (prefer over `.Instance` chains)

`Audio` · `Save` · `Pause` · `Controls` (input, on `InputHub`) · `Localize` · `SceneSystem`

- Pause is a **key set** (`Pause.On("UI")` / `Pause.Off("UI")` — stored in a `HashSet<string>`), not a bool — multiple holders can keep pause.
- Save: Newtonsoft.Json → `Application.persistentDataPath/{fileName}.json` (caller omits `.json`).
- Linear volume → dB via `Mathf.Log10(x) * 20` before `AudioMixer.SetFloat`.
- `GameFeel` is a **MonoBehaviour**, not a static facade.

## GitHub Actions — deploy only, config in workflow-level `env:`

`unity-itchio-deploy.yml` declares config in a workflow-level `env:` block; `run:` steps read `"$NAME"` only, never hardcoded values. (Drive `retrieve.yml`/`track.yml` removed along with `retrieve.py`/`track.py`/`requirements.txt` — do not re-add unless asked.)

```yaml
env:
  # Non-credential (Settings → Variables), Gemini values use || defaults
  GEMINI_PERSONA: ${{ vars.GEMINI_PERSONA }}
  ...
  # Credentials (Settings → Secrets)
  GEMINI_API_KEY: ${{ secrets.GEMINI_API_KEY }}
  DISCORD_WEBHOOK_URL_DEPLOY: ${{ secrets.DISCORD_WEBHOOK_URL_DEPLOY }}
```

`secrets.*` still appears inline in **action `with:` inputs** (Unity license activation, Butler upload) and in **failure-notify step `env:`** blocks — that is intentional, not drift.

**Secrets** (credentials only): `UNITY_EMAIL`, `UNITY_PASSWORD`, `BUTLER_API_KEY`, `GEMINI_API_KEY`, `DISCORD_WEBHOOK_URL_DEPLOY`.

**Variables** (non-credential): `GEMINI_MODEL`, `GEMINI_PERSONA`, `GEMINI_LANGUAGE` (all have `||` defaults), `ITCH_*`, `GAME_DISPLAY_NAME`, `UNITY_PROJECT_PATH`.

**Deploy inputs** (`unity-itchio-deploy.yml`, manual): `platforms` (All/Windows/macOS/WebGL) · `release_type` (release/beta/alpha) · `release_tag` (type `1.2.3`; the `v` prefix and `-{release_type}` suffix are added **automatically** → `v1.2.3-release` / `v1.2.3-beta` / `v1.2.3-alpha`). Leave `release_tag` empty for an unversioned build (no GitHub Release). The last published version is fetched via `gh api releases/latest` and shown in the log + Discord.

**Deploy changelogs** — two steps build a `git log` changelog (since previous tag, else `HEAD~1..HEAD`): the Discord notification uses it for Gemini **flavor text**; the GitHub Release body is written by **Gemini** from the same commit list. No `generate_release_notes`.

## Unity / git gotchas

- **No Git LFS.** All binaries are **plain git blobs**; `.gitattributes` marks them `binary` (`-diff -merge -text`). Do not reintroduce `filter=lfs` or `lfs: true` in workflow checkouts. Working tree may show dirty `_Vendor` files — **do not commit vendor churn**.
- `.gitattributes` sets `merge=unityyamlmerge` for `*.unity`/`*.asset`/etc., but the **driver is not in git config** — Unity YAML conflict merges fail until UnityYAMLMerge is configured.
- Serialization: Force Text YAML + Visible Meta Files. **Commit `.meta` with every new asset**; never hand-edit GUIDs.
- Input System only (`activeInputHandler: 1` in `ProjectSettings.asset`; 0=old, 1=Input System only, 2=Both). Actions: `Assets/_Game/05_Settings/Input/Main Input.inputactions` → generated `Main Input.cs` (regenerate in Unity after editing actions). Legacy `Input.GetKeyDown` in `DIalogHUD` is dead code.
- `[GameAssetCreator(category, displayName, order)]` attribute exists (`Utils/Attributes/GameAssetCreatorAttributes.cs`) but the **creator EditorWindow is gone** — nothing consumes the attribute. Other SOs use `[CreateAssetMenu]`; `SceneReference` uses `menuName: "Scene Reference"`.
- `EditorBuildSettings` lists **10 scenes** (`00_Boot` … `09_SettingsMenu`, all enabled). `10_StoryMenu`/`11_StoryScene` are on disk only.
- `*.csproj` / `*.sln` are Editor-generated (`Assembly-CSharp.csproj` is gitignored) — never hand-edit or commit them.
- **No git hooks.** `.githooks/pre-push` and `scripts/check-compile.sh` were removed deliberately — do not recreate them; local verification is the raw batchmode command below.

## Verify changes

- **Style only:** `.editorconfig`.
- **Compile:** run Unity batchmode directly. Pass = exit 0, log contains `Exiting batchmode successfully`, and no `error CS` lines. Editor is installed at `~/Unity/Hub/Editor/6000.3.25f1/Editor/Unity` on this machine; read the path from `ProjectSettings/ProjectVersion.txt` elsewhere.
- **CI:** no PR compile gate (`unity-compile.yml` deleted in PR #8 — do not re-add unless asked). The only CI compile check is inside deploy's Unity build (`error CS` fails the build). **Stale docs:** `README.md`, `GITHUB_WORKFLOWS.md`, and `CONTRIBUTING.md` still claim the PR gate exists — trust `.github/workflows/` over them.

```bash
~/Unity/Hub/Editor/6000.3.25f1/Editor/Unity \
  -batchmode -nographics -quit \
  -projectPath "$PWD" -logFile /tmp/unity-compile.log
grep -n "error CS" /tmp/unity-compile.log
grep -q "Exiting batchmode successfully" /tmp/unity-compile.log && echo OK
```

Prefer this (or opening the Editor) after any C# change. Play-mode behavior still needs a manual Editor run (`00_Boot`).
