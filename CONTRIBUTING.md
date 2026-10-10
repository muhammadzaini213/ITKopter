# Contributing

Thanks for your interest in improving this project. This document explains how to get set up and what we expect from contributions.

## Getting started

1. Fork the repository and clone it:
   ```bash
   git clone https://github.com/<your-username>/<repo>.git
   cd <repo>
   ```
2. Open the project in **Unity Hub** (version in `ProjectSettings/ProjectVersion.txt`) and let it import.
3. Create a branch for your change:
   ```bash
   git checkout -b feat/my-change
   ```

## Development setup

- **Unity version**: see `ProjectSettings/ProjectVersion.txt` (currently Unity 6.3 LTS, `6000.3.25f1`). Use the same major.minor version to avoid serialized asset churn.
- **No Git LFS** — binaries (png/ttf/audio/models…) are stored as plain git blobs. `.gitattributes` marks them `binary` so git skips diffs/merges. Never reintroduce `filter=lfs`.
- **No git hooks**: the pre-push compile check was removed. Verify compilation yourself (see below); CI still gates PRs.
- **Style**: root `.editorconfig` (hints only — not a gate).
- **No `.asmdef`** — everything compiles into `Assembly-CSharp`. Editor-only code must live under an `Editor/` folder.
- Read [`ARCHITECTURE.md`](./ARCHITECTURE.md) before touching scripts — it defines folder rules, base classes (`Singleton`/`GameSystem`/`Manager`), lifecycle (`Initialize` vs `PostInitialize`), and naming (System vs Manager vs Controller).
- Namespaces mirror folders: `Slafurry.Systems.*`, `Slafurry.Core.*`, `Slafurry.Utils.*`. The folder is `Systems/` (plural).

## Making changes

- Follow the existing code style and architecture rules in `ARCHITECTURE.md`.
- Commit `.meta` files together with every new/renamed asset. Never hand-edit GUIDs.
- Do not commit vendor churn under `Assets/_Vendor/` — working-tree noise there is common; leave it alone.
- Keep commits focused; use clear messages (e.g. `feat(ui): add pause menu`, `fix(audio): respect volume stack`).
- Do not edit files under `Assets/_Vendor/` (third-party).

## Verifying your changes

Catch compile errors headlessly:

```bash
~/Unity/Hub/Editor/6000.3.25f1/Editor/Unity \
  -batchmode -nographics -quit \
  -projectPath "$PWD" -logFile /tmp/unity-compile.log
grep -n "error CS" /tmp/unity-compile.log   # must print nothing
```

CI runs the same batchmode compile on **pull requests** (`unity-compile.yml`) and fails on `error CS`. Play-mode behavior still needs a manual Editor run (`Assets/_Game/04_Scenes/Boot.unity`).

If you change Input actions, regenerate `Main Input.cs` in the Unity Editor after editing `Assets/_Game/05_Settings/Input/Main Input.inputactions`.

## Pull requests

- Target the `main` branch.
- Describe **what** changed and **why**.
- Include screenshots/video for visual changes where relevant.
- The PR compile check must pass (or run the batchmode compile above locally if CI secrets are unavailable).
- One logical change per PR; split unrelated work.

## Reporting issues

Open a GitHub issue with:

- What you expected vs what happened
- Steps to reproduce
- Unity version and platform (Windows/macOS/Web)

## Questions?

Email **slafurrystudios@gmail.com** or open an issue.
