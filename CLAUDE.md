# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

@AGENTS.md

`AGENTS.md` (imported above) is the main reference: build/verify commands, folder layout, boot lifecycle, facades, git/Unity gotchas. This file adds the cross-file gameplay architecture and corrections where `AGENTS.md` has drifted.

## Corrections to AGENTS.md

- **Build Settings now lists 13 enabled scenes**: `00_Boot` … `11_StoryScene` plus `12_ObjectiveMenu`. `10_StoryMenu` and `11_StoryScene` *are* loadable.
- `00_Boot` also has root systems `StorySystem` (child `StoryOverlay` canvas: toasts, VICTORY/DEFEAT banner, boss health bars) and `ObjectiveSystem`.
- `00_Scripts/Game/` also has `12_Objective`.
- The animal `*.md` files at the repo root (CRAB, LOBSTER, OCTOPUS, SHELL, SHRIMP, SQUID) are jokes, not project docs.

## Verifying in a running Editor (preferred over batchmode)

The Unity CLI (`unity` on PATH, `unity-cli` skill) drives the open Editor:

- `unity recompile --project-path "$PWD" --timeout 300`: recompile and list errors.
- `unity command script eval --caller plugin --skill unity-cli --project-path "$PWD" --file x.cs`: run C# in the Editor (scene/prefab/asset edits, renders).
- `unity command editor.playmode start|stop …`: Play mode tests.

Rules that keep the user's work safe:

- Never edit or save a scene that is open with unsaved changes. Open others with `EditorSceneManager.OpenScene(path, OpenSceneMode.Additive)`, save, then `CloseScene(sc, true)`. Use try/finally so a throw doesn't leave scenes open.
- `EditorSceneManager.OpenScene` is illegal during Play mode. Check `Application.isPlaying` first, and don't stop a Play session the user is driving.
- For Play-mode tests, set `EditorSceneManager.playModeStartScene` to `00_Boot`, and reset it to `null` afterwards.
- Back up `Application.persistentDataPath` (`~/.config/unity3d/Slafurry Studios/Concept Wars`) before tests that write saves, and restore it after (with Play mode stopped).
- If the Editor is closed, run one-off editor code with batchmode `-executeMethod` from a temporary script under `Assets/Editor/`, and delete the script and its `.meta` afterwards.
- Visual checks: temporarily switch the Canvas to ScreenSpaceCamera, render the scene camera into a RenderTexture, and save a PNG. In `05_GameplayScene` the persistent `StoryOverlay` (ScreenSpaceOverlay) does not show up in such renders, so verify its state through code instead.

## Gameplay architecture (cross-file)

**Run flow.** `LevelSystem.PrepareLevel(LevelData, team)` stores the session, then `05_GameplayScene` runs:

1. `GameplayInitializer` spawns the team (index 0 starts active).
2. `LevelAreaController` and `AreaLoopController` scroll two 10-unit area tiles built from `LevelPartData.AreaPrefab`.
3. `EnemyWaveManager` instantiates each `WaveData.EnemyCollectionPrefab` in turn and waits until every `Health` in it is dead or destroyed.
4. `GameplayManager.HandleLevelWon` saves the level as completed (`PlayerSystem.CompleteLevel` → unlocks the next level via `LevelData.previousLevel`), grants `RewardEntries` with amounts, and returns to stage select.

`GameOverTrigger` handles a party wipe.

**Enemies are data-driven.** `EnemyDataBinder` (execution order -100) pushes an `EnemyData` asset onto `Health`, `EnemyMovement`, `EnemyShooter` (pattern slots plus an optional phase 2 below an HP threshold) and `EnemyLifecycle`.

- Stats are scaled by `LevelData.EnemyStatMultiplier`.
- Minion sprites are white. The binder multiplies them by `LevelData.EnemyTint` (the boss's colour) before `SquashBlink` caches its base colours.
- Bosses carry `EnemyBoss`, which skips the tint, feeds `BossHealthBarUI`, and raises `OnBossAppeared` for `LevelData.BossIntroToasts`.
- The wave container in `05_GameplayScene` is scaled ×2.9. Local positions in wave prefabs are `(world - (-0.8, -0.89)) / 2.9`, and enemy visuals appear 2.9× their prefab size.

**Story and onboarding (`Game/11_Story`).**

- A `StoryData` is a list of `StorySegment`s: Dialogue lines, or a Battle (level plus a fixed team, optional `scriptedLoss`).
- `StorySystem` (persistent) moves between `11_StoryScene` (`StorySceneController` → `StoryDialogueUI`) and the gameplay scene. There, `GameplayManager` adds `StoryBattleController`, which intercepts victory and defeat and reports back to `StorySystem`.
- Rewards are granted on the first clear only.
- The opening chapters (`Data/10_Story/Main`, chained via `nextStory`/`previousStory`) are `StorySystem.onboardingChapters`. The main menu's `OnboardingLauncher` auto-runs the next unfinished one after first-login name entry (`FirstLoginNamePopupUI`). After onboarding it points the player at Combat until `StageSelectTutorial` has run.
- Leaving a story battle by any other scene load abandons the story.

**Shared "completed" set.** `PlayerSystem.CompleteLevel` / `IsLevelCompleted` store level ids (`stage_01`…), story ids (`story_main_00`…) and flags such as `tutorial_stage_select` in one list. Ids must stay unique and stable.

**Objectives (`Game/12_Objective`).**

- `ObjectiveDatabase` asset (`Data/12_Objective`), periods Daily, Weekly, Monthly and Achievement.
- `ObjectiveSystem` saves `objectives.json` and resets periods at local midnight, on Monday, and on the 1st of the month.
- Event-based progress comes from `Objectives.Report(type, n)` calls in Energy, Currency, Player (upgrade), Shop, Gacha (`OnPullCompleted`), `EnemyLifecycle` and `GameplayManager`.
- State-based types (owned characters, character level, cleared stage or story) are re-read from `PlayerSystem`.

**Rewards with amounts.** `StoryRewardEntry { reward, amount }` is reused by stories, levels and objectives. Show results with `RewardSystem.ShowRewardPopup(rewards, amounts)`, and wait on `RewardSystem.IsPopupOpen` if the flow must pause.

**Content locations.**

| Content | Location |
|---|---|
| Main stages 1–12 (boss every 3rd) | `Data/06_Level/Stages` |
| Story battles | `Data/06_Level/Story` |
| Boss data | `Data/11_Enemy/Bosses` |
| Boss prefabs | `Prefabs/Enemy/Base/Bosses` (a minion shell wearing the character's gameplay sprite) |
| Tuned minion variants | `Data/11_Enemy/Story` (`S_*`) |

## UI conventions and pitfalls

- **Look:**
  - Font: Abaddon Bold TMP (`02_Art/Fonts/Abaddon Bold.asset`).
  - Frames: the `9Slice_0` sprite.
  - Cyan `(0.098, 0.941, 1)`, light frame tint `(0.55, 0.95, 1)`, portrait border `(0, 0.98, 1)`.
  - Rarity colours: `RarityStyle`.
  - Speaker portraits use `CharacterData.CharacterPortrait` (`_0`); icons use `Icon` (`_1`).
- **Placeholders:** many scene placeholder Images ship with alpha 0 or the Sliced type. Code that assigns a sprite at runtime should also set `color = white`, the `Simple` type and `preserveAspect`.
- **Filled images:** an `Image` of type Filled needs a sprite, or it ignores `fillAmount`. Use `02_Art/UI/Icons/Fill_White.png`, not the hollow `9Slice` sprite.
- **Speaker toasts and highlights:** use `StorySystem.Instance.Toasts.Show/PlaySequence(StoryToastLine…)`. Highlight a UI element with `UIAttentionPulse.Attach` (an overlay glow, so it never fights `UIHoverScale`).
- **Asset scripts:** when regenerating assets from a script, `EditorUtility.CopySerialized` onto an existing asset also copies `m_Name`. Restore `name` from the file name afterwards.
