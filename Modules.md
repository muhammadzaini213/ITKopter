# Concept Wars: Modules

A **module** is a group of scripts that does one job and is loosely coupled to the others. This file lists every module, its scripts, and each script's public members **that other classes actually call**.

- Only scripts that are used are listed (checked against scenes, prefabs, assets and code).
- "Used by" comes from the compiled game code (every method's calls were read), so it lists real calls only. Calls between classes in the same file are left out.
- A script with no table is a component wired up in scenes or prefabs and driven by Unity itself (`Start`, `Update`, button events).
- Paths are relative to `Assets/_Game/00_Scripts/`. Regenerate this file after big changes; it is a snapshot.

## Contents

- **Foundation**: [Boot / Lifecycle](#boot--lifecycle), [Scene](#scene), [Save](#save), [Audio](#audio), [Pause](#pause), [Pooling](#pooling), [UI kit](#ui-kit)
- **Meta game**: [Player Profile](#player-profile), [Currency](#currency), [Energy](#energy), [Inventory / Items](#inventory--items), [Reward](#reward), [Gacha](#gacha), [Shop](#shop), [Top-up (Bundle)](#top-up-bundle), [Account / Cloud Save](#account--cloud-save), [Objectives](#objectives), [Characters (data)](#characters-data), [Stage Select / Level](#stage-select--level), [Story](#story), [Character flavour](#character-flavour)
- **Combat**: [Battle flow](#battle-flow), [Play area / Camera](#play-area--camera), [Characters (in battle)](#characters-in-battle), [Party Switching](#party-switching), [EX Skills](#ex-skills), [Annihilator](#annihilator), [Enemies](#enemies), [Projectiles](#projectiles), [Health / Damage](#health--damage), [Hierarchy counters](#hierarchy-counters), [Status effects](#status-effects), [Combat events](#combat-events), [Combat feel / VFX](#combat-feel--vfx), [Combat HUD](#combat-hud)

---

# Foundation

## Boot / Lifecycle

Boots every system in priority order.

### `Core/Abstract/GameSystem.cs`

_Base class for cross-scene systems (kept alive between scenes); used by inheritance._

### `Core/Abstract/Singleton.cs`

Every system reaches the others through `Instance`. This table shows who uses which system:

| Member | Used by |
|---|---|
| `AccountSystem.Instance` | AccountMenuPrompt, AccountPopupUI, AccountSettingsRow, BundleSystem |
| `AudioSystem.Instance` | SettingsVolumeUI, StorySceneController, UIButtonSFX |
| `BundleSystem.Instance` | AccountSystem, BundleComponentUI, BundlePurchasePopupUI |
| `CurrencySystem.Instance` | CharacterDetailPanelUI, CurrencyComponentUI, EnergyRecoveryPopupUI, GachaComponentUI, GachaSystem +3 more |
| `EnergySystem.Instance` | CurrencyComponentUI, EnergyRecoveryPopupUI, StageSweep, StageSweepPopupUI, TeamSelectionPopupUI |
| `GachaSystem.Instance` | GachaComponentUI, GachaRatesPopupUI, ObjectiveSystem, StorySystem |
| `InventorySystem.Instance` | BuyConfirmationPopupUI, CharacterDetailPanelUI, InventoryUI, PlayerSystem, ProfileSummaryUI +3 more |
| `LevelSystem.Instance` | EnemyDataBinder, EnemyWaveManager, GameplayInitializer, GameplayManager, LevelAreaController +2 more |
| `ObjectiveSystem.Instance` | ObjectiveBadgeUI, ObjectiveMenuUI, Objectives, StorySystem |
| `PauseSystem.Instance` | CombatFeelRunner, ExSkillController, Pause, StorySystem |
| `PlayerSystem.Instance` | AccountSystem, AnnihilatorManifestation, Character, CharacterMenuUI, CharacterTapDialogue +23 more |
| `RewardSystem.Instance` | BundleSystem, GachaComponentUI, GachaSystem, GameplayManager, ObjectiveSystem +3 more |
| `SaveSystem.Instance` | AccountSystem, Save |
| `SceneLoader.Instance` | MusicPlayer, SceneSystem, StorySystem |
| `ShopSystem.Instance` | BuyConfirmationPopupUI, ShopComponentUI, StorySystem |
| `StorySystem.Instance` | AnnihilatorManifestation, CharacterTapDialogue, ExSkillController, GameplayManager, ObjectiveMenuUI +8 more |

### `Core/Abstract/LocalSingleton.cs`

_Base class for single-scene singletons; used by inheritance._

### `Core/Interface/IInitializable.cs`

| Member | Used by |
|---|---|
| `int Priority` | LoadingSystem |
| `IEnumerator Initialize()` | LoadingSystem |
| `void PostInitialize()` | LoadingSystem |

### `Systems/Loading/LoadingSystem.cs`

| Member | Used by |
|---|---|
| `void Register(IInitializable obj)` | Singleton |

### `Systems/Scene/BootstrapLoader.cs`

_Scene/prefab component: no members called by other classes._

## Scene

Loads scenes by reference.

### `Systems/Scene/SceneLoader.cs`

| Member | Used by |
|---|---|
| `event Action<string> SceneLoader.OnSceneLoadCompleted` | MusicPlayer, StorySystem |
| `static void SceneSystem.Load(SceneReference scene)` | BootstrapLoader, GameOverUI, GameplayManager, GameplayPauseMenu, StorySelectionUI +2 more |

### `Systems/Scene/SceneReference.cs`

| Member | Used by |
|---|---|
| `string SceneName` | SceneLoader, SceneTrack, StorySystem |

## Save

Local JSON save files (WebGL: browser storage).

### `Systems/Save/SaveSystem.cs`

| Member | Used by |
|---|---|
| `static T Save.From(string fileName, T fallback = null)` | AccountSystem, CurrencySystem, EnergySystem, GachaSystem, ObjectiveSystem +2 more |
| `static bool Save.Exists(string fileName)` | EnergySystem |
| `static void Save.To(T data, string fileName)` | CurrencySystem, EnergySystem, GachaSystem, ObjectiveSystem, PlayerSystem +1 more |
| `event Action<string> SaveSystem.OnSaved` | AccountSystem |
| `List<string> SaveSystem.SaveFileNames()` | AccountSystem |
| `static void SaveSystem.Flush()` | AccountSystem |
| `string SaveSystem.ReadRaw(string fileName)` | AccountSystem |
| `void SaveSystem.BlockSaves()` | AccountSystem |
| `void SaveSystem.SaveAccountFile(T data, string fileName)` | AccountSystem |
| `void SaveSystem.WriteRaw(string fileName, string json)` | AccountSystem |

## Audio

Music and sound effects.

### `Systems/Audio/AudioSystem.cs`

| Member | Used by |
|---|---|
| `static void Audio.PlayMusic(MusicReference music, float fade = -1)` | LevelSystem, StorySceneController |
| `static void Audio.PlaySFX2D(SFXReference sfx, bool loop = false, float fadeIn = -1)` | BulletSpawner, CharacterTapDialogue, EnergyRecoveryPopupUI, ExSkillController, ExpGainBarUI +6 more |
| `static SFXPlayer AudioSystem.SFX` | UIButtonSFX |
| `static float AudioSystem.DecibelToLinear(float db)` | SettingsVolumeUI |
| `static float AudioSystem.LinearToDecibel(float linear)` | SettingsVolumeUI |
| `void AudioSystem.UpdateMusicVolume(float db)` | SettingsVolumeUI |
| `void AudioSystem.UpdateSFXVolume(float db)` | SettingsVolumeUI |

### `Systems/Audio/Music/MusicData.cs`

| Member | Used by |
|---|---|
| `AudioClip GetClipFromName(string trackName)` | MusicPlayer |
| `MusicTrack GetTrack(string trackName)` | MusicPlayer |

### `Systems/Audio/Music/MusicPlayer.cs`

| Member | Used by |
|---|---|
| `void Initialize()` | AudioSystem |
| `void PlayMusic(string trackName, float fadeDuration = 0)` | Audio, AudioSystem |
| `void StopMusic(float fadeDuration = 0)` | Audio |

### `Systems/Audio/Music/MusicReference.cs`

| Member | Used by |
|---|---|
| `string MusicName` | Audio, MusicPlayer, MusicTrack, SceneTrack |

### `Systems/Audio/Music/MusicTrack.cs`

| Member | Used by |
|---|---|
| `AudioClip MusicTrack.clip` | MusicData, MusicPlayer |
| `AudioClip MusicTrack.introClip` | MusicPlayer |
| `MusicReference MusicTrack.music` | MusicData, MusicPlayer |
| `float MusicTrack.volume` | MusicPlayer |
| `string MusicTrack.TrackName` | MusicData |
| `string SceneTrack.SceneName` | MusicPlayer |
| `string SceneTrack.TrackName` | MusicPlayer |

### `Systems/Audio/SFX/SFXCategory.cs`

| Member | Used by |
|---|---|
| `SFXEffect[] SFXCategory.effects` | SFXData |
| `int SFXCategory.poolSize` | SFXPlayer |
| `string SFXCategory.categoryName` | SFXData, SFXPlayer |
| `AudioClip[] SFXEffect.clips` | SFXPlayer |
| `AudioRolloffMode SFXEffect.rolloffMode` | SFXPlayer |
| `bool SFXEffect.distanceOnlyNoPanning` | SFXPlayer |
| `float SFXEffect.fadeIn` | SFXPlayer |
| `float SFXEffect.fadeOut` | SFXPlayer |
| `float SFXEffect.maxDistance` | SFXPlayer |
| `float SFXEffect.minDistance` | SFXPlayer |
| `float SFXEffect.volume` | SFXPlayer |
| `int SFXEffect.maxSimultaneous` | SFXPlayer |
| `string SFXEffect.groupID` | SFXData |

### `Systems/Audio/SFX/SFXData.cs`

| Member | Used by |
|---|---|
| `SFXCategory[] categories` | SFXPlayer |
| `SFXEffect GetSFXEffect(string categoryName, string effectName)` | SFXPlayer |

### `Systems/Audio/SFX/SFXPlayer.cs`

| Member | Used by |
|---|---|
| `void PlaySFX2D(string categoryName, string effectName, bool loop = false, float fadeIn = -1)` | Audio, UIButtonSFX |
| `void PlaySFX3D(string categoryName, string effectName, Vector3 pos, bool loop = false, float fadeIn = -1)` | Audio |
| `void StopAllSFX(float fadeOut = -1)` | Audio |
| `void StopCategory(string categoryName, float fadeOut = -1)` | Audio |
| `void StopSFX(string categoryName, string effectName, float fadeOut = -1)` | Audio |

### `Systems/Audio/SFX/SFXReference.cs`

| Member | Used by |
|---|---|
| `string Category` | Audio |
| `string SFXName` | Audio |

### `UI/Components/Settings/SettingsVolumeUI.cs`

_Scene/prefab component: no members called by other classes._

## Pause

Pause with a key set (several holders can pause at once).

### `Systems/Pause/PauseSystem.cs`

| Member | Used by |
|---|---|
| `static void Pause.ForceResume()` | StorySystem |
| `static void Pause.Off(string key = "Global")` | AnnihilatorManifestation, GameplayPauseMenu, TradyExchangeChoiceUI |
| `static void Pause.On(string key = "Global")` | AnnihilatorManifestation, GameplayPauseMenu, TradyExchangeChoiceUI |
| `bool PauseSystem.IsPaused` | CombatFeelRunner, ExSkillController |

## Pooling

Reuses objects such as projectiles.

### `Utils/Pool/GenericPool.cs`

| Member | Used by |
|---|---|
| `Projectile Get()` | BulletSpawner |
| `new GenericPool`1(Projectile prefab, Transform parent = null, int defaultCapacity = 10, int maxSize = 100, bool collectionCheck = true)` | BulletSpawner |
| `void Clear()` | BulletSpawner |
| `void Prewarm(int count)` | BulletSpawner |
| `void Release(Projectile item)` | BulletSpawner |
| `void Release(VFXCleaner item)` | VFXCleaner |

### `Utils/Pool/IPoolAble.cs`

| Member | Used by |
|---|---|
| `void OnDespawn()` | GenericPool |
| `void OnSpawn()` | GenericPool |

### `Utils/Pool/PoolParent.cs`

_Scene/prefab component: no members called by other classes._

## UI kit

Reusable UI behaviours and popups built at runtime.

### `Utils/UI/UIButtonSFX.cs`

_Scene/prefab component: no members called by other classes._

### `Utils/UI/UIFade.cs`

_Scene/prefab component: no members called by other classes._

### `Utils/UI/UIFloat.cs`

_Scene/prefab component: no members called by other classes._

### `Utils/UI/UIHoverScale.cs`

_Scene/prefab component: no members called by other classes._

### `Utils/UI/UIParallax.cs`

_Scene/prefab component: no members called by other classes._

### `UI/Generic/RuntimeUIKit.cs`

| Member | Used by |
|---|---|
| `static Color Cyan` | AccountPopupUI, BundleComponentUI, StageSweepPopupUI, StoryEndPopupUI |
| `static Color Dim` | AccountPopupUI, StageSweepPopupUI, StoryEndPopupUI |
| `static Color SubtleText` | AccountPopupUI, StageSweepPopupUI, StoryEndPopupUI |
| `static Button NewButton(string name, Transform parent, TMP_FontAsset font, Sprite frame, string label, float size, Vector2 position, Vector2 box, float pixelsPerUnit = 1.5)` | AccountPopupUI, AccountSettingsRow, AnnihilatorHierarchyChoiceUI, BundleComponentUI, StageSweepPopupUI +1 more |
| `static GameObject NewOverlayCanvas(string name, int sortingOrder)` | AccountPopupUI, StageSweepPopupUI, StoryEndPopupUI |
| `static Image NewImage(string name, Transform parent, Color color, Sprite sprite, float pixelsPerUnit = 1)` | AccountPopupUI, AnnihilatorHierarchyChoiceUI, StageSweepPopupUI, StoryEndPopupUI |
| `static Sprite FindFrameSprite()` | AccountPopupUI, AccountSettingsRow, AnnihilatorHierarchyChoiceUI, BundleComponentUI, StageSweepPopupUI +1 more |
| `static TMP_FontAsset FindFont()` | AccountPopupUI, AccountSettingsRow, BundleComponentUI, StageSweepPopupUI, StoryEndPopupUI |
| `static TMP_Text NewText(string name, Transform parent, TMP_FontAsset font, string text, float size, Color color, Vector2 position, Vector2 box)` | AccountPopupUI, AccountSettingsRow, AnnihilatorHierarchyChoiceUI, StageSweepPopupUI, StoryEndPopupUI |
| `static void SetLabel(Button button, string label)` | AccountSettingsRow |
| `static void Stretch(RectTransform rt)` | AccountPopupUI, AnnihilatorHierarchyChoiceUI, StageSweepPopupUI, StoryEndPopupUI |

### `UI/Generic/InfoPopupUI.cs`

| Member | Used by |
|---|---|
| `static Button CloneButton(Button template, Transform parent, string name, string label, Action onClick)` | CharacterDetailPanelUI, TeamSelectionPopupUI |
| `static void Show(string title, string body, Vector2 size = null, SFXReference clickSfx = null)` | CharacterDetailPanelUI, TeamSelectionPopupUI |

### `UI/Generic/UIAttentionPulse.cs`

| Member | Used by |
|---|---|
| `static UIAttentionPulse Attach(GameObject target)` | AnnihilatorHierarchyChoiceUI, ObjectiveRowUI, OnboardingLauncher, StageSelectTutorial, StoryEndPopupUI |
| `static void Detach(GameObject target)` | ObjectiveRowUI, StageSelectTutorial |


---

# Meta game

## Player Profile

Name, EXP and level, owned characters, completed ids, last team.

### `Game/00_Player/PlayerSystem.cs`

| Member | Used by |
|---|---|
| `int ExpGain.Amount` | ExpGainBarUI |
| `int ExpGain.ExpBefore` | ExpGainBarUI |
| `event Action PlayerSystem.OnCharacterOwnedChanged` | CharacterMenuUI, MainMenuShowcaseUI, ObjectiveSystem, ProfileSummaryUI |
| `event Action PlayerSystem.OnLevelProgressChanged` | LevelSelectionUI, ObjectiveSystem, ProfileSummaryUI, StoryBadgeUI, StorySelectionUI |
| `event Action PlayerSystem.OnPlayerDataReady` | CharacterMenuUI, FirstLoginNamePopupUI, LevelSelectionUI, MainMenuShowcaseUI, ObjectiveSystem +4 more |
| `event Action PlayerSystem.OnShowcaseCharacterChanged` | MainMenuShowcaseUI, ProfileSummaryUI |
| `event Action<int> PlayerSystem.OnPlayerExpChanged` | PlayerComponentUI |
| `event Action<int> PlayerSystem.OnPlayerLevelChanged` | PlayerComponentUI |
| `event Action<string> PlayerSystem.OnCharacterProgressionChanged` | CharacterMenuUI, ObjectiveSystem |
| `event Action<string> PlayerSystem.OnPlayerNameChanged` | OnboardingLauncher, PlayerComponentUI |
| `CharacterData PlayerSystem.ShowcaseCharacter` | CharacterTapDialogue, MainMenuShowcaseUI, ProfileSummaryUI, ShowcasePickerUI |
| `CharacterDatabase PlayerSystem.CharacterDatabase` | ObjectiveSystem, ProfileSummaryUI, ShopSystem, ShowcasePickerUI |
| `IReadOnlyCollection<string> PlayerSystem.CompletedLevelIds` | AccountSystem, ProfileSummaryUI |
| `IReadOnlyList<CharacterData> PlayerSystem.CharacterOwned` | CharacterMenuUI, HierarchyGuide, LevelEnemyIntel, ProfileSummaryUI, TeamSelectionPopupUI |
| `bool PlayerSystem.HasPendingExpGain` | GameplayManager, RewardPopupUI |
| `bool PlayerSystem.IsPlayerDataLoaded` | AccountSystem, CharacterMenuUI, LevelSelectionUI, ObjectiveSystem, OnboardingLauncher +6 more |
| `bool PlayerSystem.NeedsNameEntry` | FirstLoginNamePopupUI, OnboardingLauncher |
| `int PlayerSystem.CurrentLevelExp` | PlayerComponentUI |
| `int PlayerSystem.PlayerLevel` | AccountSystem, AnnihilatorManifestation, PlayerComponentUI |
| `int PlayerSystem.RequiredLevelExp` | PlayerComponentUI |
| `string PlayerSystem.PlayerName` | AccountSystem, PlayerComponentUI, StoryDialogueUI |
| `CharacterProgression PlayerSystem.GetCharacterProgression(CharacterData character)` | CharacterMenuUI, TeamSelectionPopupUI |
| `CharacterUpgradeCost PlayerSystem.GetUpgradeCost(CharacterData character)` | CharacterMenuUI |
| `List<CharacterData> PlayerSystem.GetLastTeamCharacters()` | TeamSelectionPopupUI |
| `bool PlayerSystem.AddCharacterAffection(string characterId, int amount)` | CharacterMenuUI |
| `bool PlayerSystem.AddNewOwnedCharacter(CharacterData newCharacter)` | RewardSystem, ShopSystem |
| `bool PlayerSystem.CanAddNewOwnedCharacter(CharacterData character)` | RewardSystem |
| `bool PlayerSystem.CanUpgradeCharacter(CharacterData character)` | CharacterMenuUI |
| `bool PlayerSystem.IsCharacterOwned(CharacterData character)` | GachaSystem, ObjectiveSystem, ShopSystem, ShowcasePickerUI |
| `bool PlayerSystem.IsLevelCompleted(string levelId)` | LevelSelectionUI, ObjectiveSystem, StageSelectTutorial, StageSweep, StoryProgress +2 more |
| `bool PlayerSystem.IsLevelUnlocked(LevelData level)` | LevelSelectionUI, TeamSelectionPopupUI |
| `bool PlayerSystem.SaveLastTeam(IReadOnlyList<CharacterData> team)` | TeamSelectionPopupUI |
| `bool PlayerSystem.SaveProgress()` | ShopSystem |
| `bool PlayerSystem.SetShowcaseCharacter(CharacterData character)` | ShowcasePickerUI |
| `bool PlayerSystem.TryConsumeExpGain(out ExpGain gain)` | ExpGainBarUI |
| `bool PlayerSystem.UpgradeCharacter(CharacterData character)` | CharacterMenuUI |
| `int PlayerSystem.GetCharacterLevel(string characterId)` | Character, ObjectiveSystem |
| `int PlayerSystem.GrantLevelExp(LevelData level)` | GameplayManager |
| `int PlayerSystem.GrantSweepExp(LevelData level, int count)` | StageSweep |
| `static void PlayerSystem.GetLevelProgress(long totalExp, out int level, out int currentExp, out int requiredExp)` | ExpGainBarUI |
| `void PlayerSystem.CompleteLevel(string levelId)` | GameplayManager, StageSelectTutorial, StorySystem |
| `void PlayerSystem.SetPlayerName(string newName)` | FirstLoginNamePopupUI |

### `Game/00_Player/PlayerSaveData.cs`

| Member | Used by |
|---|---|
| `int CharacterProgressionSaveData.affectionPoints` | PlayerSystem |
| `int CharacterProgressionSaveData.level` | PlayerSystem |
| `string CharacterProgressionSaveData.characterId` | PlayerSystem |
| `new CharacterProgressionSaveData()` | PlayerSystem |
| `List<CharacterProgressionSaveData> PlayerSaveData.characterProgressions` | PlayerSystem |
| `List<InventoryItemSaveData> PlayerSaveData.inventoryItems` | InventorySystem |
| `List<string> PlayerSaveData.completedLevelIds` | PlayerSystem |
| `List<string> PlayerSaveData.lastTeamCharacterIds` | PlayerSystem |
| `List<string> PlayerSaveData.ownedCharacterIds` | PlayerSystem |
| `bool PlayerSaveData.hasChosenName` | PlayerSystem |
| `int PlayerSaveData.playerExp` | PlayerSystem |
| `string PlayerSaveData.playerName` | PlayerSystem |
| `string PlayerSaveData.showcaseCharacterId` | PlayerSystem |
| `new PlayerSaveData()` | PlayerSystem |

### `UI/Components/Player/PlayerComponentUI.cs`

_Scene/prefab component: no members called by other classes._

### `UI/Components/Player/ProfileSummaryUI.cs`

_Scene/prefab component: no members called by other classes._

### `UI/Components/Player/ExpGainBarUI.cs`

| Member | Used by |
|---|---|
| `bool PlayPendingGain()` | RewardPopupUI |

### `UI/Components/Player/FirstLoginNamePopupUI.cs`

_Scene/prefab component: no members called by other classes._

### `UI/Components/Player/MainMenuShowcaseUI.cs`

_Scene/prefab component: no members called by other classes._

### `UI/Components/Player/ShowcasePickerUI.cs`

_Scene/prefab component: no members called by other classes._

### `UI/Components/Player/ShowcaseOptionUI.cs`

| Member | Used by |
|---|---|
| `void Setup(CharacterData data, bool isOwned, bool isSelected, Action<CharacterData> pickedCallback)` | ShowcasePickerUI |

## Currency

Coins and Gems.

### `Game/01_Currency/CurrencySystem.cs`

| Member | Used by |
|---|---|
| `event Action<int> OnCoinsChanged` | CurrencyComponentUI |
| `event Action<int> OnGemsChanged` | CurrencyComponentUI |
| `int Coins` | CharacterDetailPanelUI, CurrencyComponentUI, PlayerSystem, ShopSystem |
| `int Gems` | CurrencyComponentUI, EnergyRecoveryPopupUI, GachaComponentUI, GachaSystem |
| `bool SpendCoins(int amount)` | PlayerSystem, ShopSystem |
| `bool SpendGems(int amount)` | EnergyRecoveryPopupUI, GachaSystem |
| `void AddCoins(int amount)` | RewardSystem, ShopSystem |
| `void AddGems(int amount)` | RewardSystem |

### `UI/Components/Currency/CurrencyComponentUI.cs`

_Scene/prefab component: no members called by other classes._

## Energy

Stamina with regeneration.

### `Game/01_Currency/EnergySystem.cs`

| Member | Used by |
|---|---|
| `event Action<int> OnEnergyChanged` | CurrencyComponentUI |
| `TimeSpan TimeUntilFull` | EnergyRecoveryPopupUI |
| `TimeSpan TimeUntilNextEnergy` | EnergyRecoveryPopupUI |
| `int Energy` | CurrencyComponentUI, EnergyRecoveryPopupUI, StageSweep, StageSweepPopupUI, TeamSelectionPopupUI |
| `int EnergyMissing` | EnergyRecoveryPopupUI |
| `int MaxEnergy` | CurrencyComponentUI, EnergyRecoveryPopupUI |
| `int RecoveryIntervalMinutes` | EnergyRecoveryPopupUI |
| `bool SpendEnergy(int amount)` | StageSweep, TeamSelectionPopupUI |
| `void AddEnergy(int amount)` | EnergyRecoveryPopupUI |

### `UI/Components/Currency/EnergyRecoveryPopupUI.cs`

| Member | Used by |
|---|---|
| `static void Show(SFXReference clickSfx = null, Slider barTemplate = null)` | CurrencyComponentUI |

## Inventory / Items

Item stacks (dust, engrams).

### `Game/10_Inventory/InventorySystem.cs`

| Member | Used by |
|---|---|
| `event Action OnInventoryChanged` | PlayerSystem |
| `bool IsDatabaseReady` | InventoryUI, PlayerSystem, ShopSystem |
| `List<InventoryItemSaveData> GetInventoryItems()` | InventoryUI, ProfileSummaryUI |
| `bool AddItem(ItemData item, int amount = 1)` | PlayerSystem, RewardSystem, ShopSystem |
| `bool CanAddItem(ItemData item, int amount)` | RewardSystem, ShopSystem |
| `bool RemoveItem(ItemData item, int amount = 1)` | PlayerSystem, ShopSystem |
| `bool TryGetItem(string itemId, out ItemData item)` | InventoryUI |
| `int GetItemAmount(ItemData item)` | BuyConfirmationPopupUI, CharacterDetailPanelUI, PlayerSystem, ShopComponentUI, ShopSystem |
| `void LoadFromSaveData(PlayerSaveData saveData)` | PlayerSystem |
| `void WriteToSaveData(PlayerSaveData saveData)` | PlayerSystem |

### `Game/10_Inventory/InventoryItemSaveData.cs`

| Member | Used by |
|---|---|
| `int amount` | InventorySystem, InventoryUI, ProfileSummaryUI |
| `string itemId` | InventorySystem, InventoryUI |
| `new InventoryItemSaveData(string itemId, int amount)` | InventorySystem |

### `Game/05_Items/ItemData.cs`

| Member | Used by |
|---|---|
| `string Description` | GachaRevealUI |
| `string ItemId` | InventorySystem, ItemDatabase |
| `string ItemName` | BuyConfirmationPopupUI, ConversionOffer, GachaRatesPopupUI, GachaRevealUI, InventorySlotUI +2 more |

### `Game/05_Items/ItemDatabase.cs`

| Member | Used by |
|---|---|
| `bool Contains(ItemData item)` | InventorySystem |
| `bool TryGetItem(string itemId, out ItemData item)` | InventorySystem |

### `UI/Components/Inventory/InventoryUI.cs`

_Scene/prefab component: no members called by other classes._

### `UI/Components/Inventory/InventorySlotUI.cs`

| Member | Used by |
|---|---|
| `void Setup(ItemData item, int amount)` | InventoryUI |

## Reward

Grants any reward and shows the reward popup.

### `Game/08_Reward/RewardSystem.cs`

| Member | Used by |
|---|---|
| `bool IsPopupOpen` | GameplayManager, StorySystem |
| `List<RewardGrantResult> GrantReward(RewardData reward, int amount = 1)` | ShopSystem |
| `List<RewardGrantResult> GrantRewards(IReadOnlyList<RewardGrantRequest> requests)` | BundleSystem, GachaSystem, GameplayManager, ObjectiveSystem, StageSweep +1 more |
| `bool CanGrantRewards(IReadOnlyList<RewardGrantRequest> requests, out string error)` | BundleSystem, GachaSystem, ObjectiveSystem, ShopSystem |
| `void ShowRewardPopup(IReadOnlyList<RewardData> rewards)` | ShopSystem |
| `void ShowRewardPopup(IReadOnlyList<RewardData> rewards, IReadOnlyList<int> amounts)` | BundleSystem, GachaComponentUI, GachaSystem, GameplayManager, ObjectiveSystem +2 more |

### `Game/08_Reward/RewardData.cs`

| Member | Used by |
|---|---|
| `RewardRarity Rarity` | CharacterDetailPanelUI, GachaPullResult, GachaSystem, InventorySlotUI, ShopSystem |
| `Sprite Icon` | BossHealthBarUI, BuyConfirmationPopupUI, Character, CharacterDetailPanelUI, GachaComponentUI +11 more |

### `Game/08_Reward/CurrencyRewardData.cs`

| Member | Used by |
|---|---|
| `CurrencyType CurrencyType` | GachaRatesPopupUI, GachaRevealUI, RewardItemUI, RewardSystem |

### `Game/08_Reward/RewardGrantRequest.cs`

| Member | Used by |
|---|---|
| `RewardData reward` | RewardSystem |
| `int amount` | RewardSystem |
| `new RewardGrantRequest(RewardData reward, int amount = 1)` | BundleSystem, GachaSystem, GameplayManager, ObjectiveSystem, RewardSystem +3 more |

### `Game/08_Reward/RewardGrantResult.cs`

| Member | Used by |
|---|---|
| `RewardData Reward` | GameplayManager, ObjectiveSystem, StageSweep, StorySystem |
| `bool Success` | BundleSystem, GachaSystem, GameplayManager, ObjectiveSystem, ShopSystem +2 more |
| `int GrantedAmount` | GameplayManager, ObjectiveSystem, StageSweep, StorySystem |
| `string Message` | GameplayManager, StageSweep, StorySystem |
| `new RewardGrantResult(RewardData reward, int requestedAmount, int grantedAmount, bool success, string message)` | RewardSystem |

### `UI/Components/Reward/RewardPopupUI.cs`

| Member | Used by |
|---|---|
| `bool IsOpen` | RewardSystem |
| `void ShowRewards(IReadOnlyList<RewardData> rewards)` | RewardSystem |
| `void ShowRewards(IReadOnlyList<RewardData> rewards, IReadOnlyList<int> rewardAmounts)` | RewardSystem |

### `UI/Components/Reward/RewardItemUI.cs`

| Member | Used by |
|---|---|
| `void Setup(RewardData reward, int amount)` | RewardPopupUI |

### `UI/Components/Reward/RarityStyle.cs`

| Member | Used by |
|---|---|
| `static Color GetColor(RewardRarity rarity)` | CharacterDetailPanelUI, InventorySlotUI |
| `static string GetFullName(RewardRarity rarity)` | CharacterDetailPanelUI |

## Gacha

Banners, pulls and pity.

### `Game/04_Gacha/GachaSystem.cs`

| Member | Used by |
|---|---|
| `event Action<GachaData, GachaPullResult[]> OnPullCompleted` | ObjectiveSystem, StorySystem |
| `List<GachaRateEntry> GetRewardRates(GachaData gacha)` | GachaRatesPopupUI |
| `bool Pull(GachaData gacha, int pullCount, bool showPopup, out GachaPullResult[] results)` | GachaComponentUI |

### `Game/04_Gacha/GachaData.cs`

| Member | Used by |
|---|---|
| `GachaEntry[] GachaData.GachaEntries` | GachaSystem |
| `RewardData GachaData.FeaturedReward` | GachaComponentUI, GachaConfirmationPopupUI, GachaRatesPopupUI, GachaSystem |
| `RewardRarity GachaData.PityRarity` | GachaRatesPopupUI, GachaSystem |
| `float GachaData.GachaDiscount` | GachaComponentUI, GachaSystem |
| `float GachaData.NRate` | GachaRatesPopupUI, GachaSystem |
| `float GachaData.RRate` | GachaRatesPopupUI, GachaSystem |
| `float GachaData.SRRate` | GachaRatesPopupUI, GachaSystem |
| `float GachaData.URRate` | GachaRatesPopupUI, GachaSystem |
| `int GachaData.GachaCost` | GachaComponentUI, GachaSystem |
| `int GachaData.MultiPullCount` | GachaComponentUI, GachaSystem |
| `int GachaData.PityThreshold` | GachaRatesPopupUI, GachaSystem |
| `string GachaData.Description` | GachaComponentUI |
| `string GachaData.GachaId` | GachaSystem |
| `string GachaData.GachaName` | GachaComponentUI, GachaConfirmationPopupUI, GachaRatesPopupUI |
| `RewardData GachaEntry.RewardData` | GachaSystem |
| `RewardRarity GachaEntry.OverrideRarityValue` | GachaSystem |
| `bool GachaEntry.OverrideRarity` | GachaSystem |
| `bool GachaEntry.UseInterval` | GachaSystem |
| `int GachaEntry.HighInterval` | GachaSystem |
| `int GachaEntry.LowInterval` | GachaSystem |
| `int GachaEntry.MaxAmount` | GachaSystem |
| `int GachaEntry.MinAmount` | GachaSystem |

### `Game/04_Gacha/GachaRateEntry.cs`

| Member | Used by |
|---|---|
| `RewardData Reward` | GachaRatesPopupUI |
| `RewardRarity Rarity` | GachaRatesPopupUI |
| `bool IsFeatured` | GachaRatesPopupUI |
| `float Percent` | GachaRatesPopupUI |
| `int MaxAmount` | GachaRatesPopupUI |
| `int MinAmount` | GachaRatesPopupUI |
| `new GachaRateEntry(RewardData reward, RewardRarity rarity, float percent, bool isFeatured, int minAmount = 1, int maxAmount = 1)` | GachaSystem |

### `Game/04_Gacha/GachaPullResult.cs`

| Member | Used by |
|---|---|
| `IReadOnlyList<RewardAmount> GachaPullResult.ConvertedRewards` | GachaRevealUI |
| `RewardData GachaPullResult.Reward` | GachaComponentUI, GachaRevealUI, GachaSystem |
| `RewardRarity GachaPullResult.Rarity` | GachaRevealUI |
| `bool GachaPullResult.IsDuplicate` | GachaRevealUI |
| `int GachaPullResult.Amount` | GachaComponentUI, GachaRevealUI, GachaSystem |
| `new GachaPullResult(RewardData reward, bool wasPity, int amount = 1, RewardRarity? rarity = null)` | GachaSystem |
| `void GachaPullResult.MarkDuplicate(IEnumerable<RewardAmount> converted)` | GachaSystem |
| `RewardData RewardAmount.Reward` | GachaRevealUI |
| `int RewardAmount.Amount` | GachaRevealUI |
| `new RewardAmount(RewardData reward, int amount)` | GachaSystem |

### `Game/04_Gacha/GachaSaveData.cs`

| Member | Used by |
|---|---|
| `int BannerPityData.pityCount` | GachaSystem |
| `string BannerPityData.gachaId` | GachaSystem |
| `new BannerPityData()` | GachaSystem |
| `List<BannerPityData> GachaSaveData.banners` | GachaSystem |
| `new GachaSaveData()` | GachaSystem |

### `UI/Components/Gacha/GachaComponentUI.cs`

_Scene/prefab component: no members called by other classes._

### `UI/Components/Gacha/GachaConfirmationPopupUI.cs`

| Member | Used by |
|---|---|
| `void Open(GachaData gacha, int pullCount, int cost, int currentGems, Action confirmCallback)` | GachaComponentUI |

### `UI/Components/Gacha/GachaRatesPopupUI.cs`

| Member | Used by |
|---|---|
| `void Open(GachaData gacha)` | GachaComponentUI |

### `UI/Components/Gacha/GachaRevealUI.cs`

| Member | Used by |
|---|---|
| `void Play(IReadOnlyList<GachaPullResult> pullResults, RewardData featured, Action finishedCallback)` | GachaComponentUI |

### `UI/Components/Gacha/GachaRevealTapArea.cs`

| Member | Used by |
|---|---|
| `event Action OnTapped` | GachaRevealUI |

## Shop

Coin purchases and conversions.

### `Game/09_Shop/ShopSystem.cs`

| Member | Used by |
|---|---|
| `event Action OnConversionCompleted` | ShopComponentUI |
| `event Action<ShopItemEntry> OnItemPurchased` | StorySystem |
| `List<ConversionOffer> GetConversionOffers(ShopData shop)` | ShopComponentUI |
| `bool BuyItem(ShopData shop, ShopItemEntry entry, int quantity, out string error)` | BuyConfirmationPopupUI |
| `bool Convert(ConversionOffer offer, int quantity, out string error)` | BuyConfirmationPopupUI |
| `int GetMaxConvertQuantity(ConversionOffer offer)` | BuyConfirmationPopupUI |
| `int GetRemainingToday(ConversionOffer offer)` | BuyConfirmationPopupUI, ShopComponentUI |

### `Game/09_Shop/ShopData.cs`

| Member | Used by |
|---|---|
| `ConversionRule[] ConversionRules` | ShopSystem |
| `ShopItemEntry[] ItemsForSale` | ShopComponentUI, ShopSystem |
| `bool IsConversionShop` | ShopComponentUI, ShopSystem |
| `string ShopName` | ShopComponentUI, ShopSystem |

### `Game/09_Shop/ShopItemEntry.cs`

| Member | Used by |
|---|---|
| `RewardData RewardData` | BuyConfirmationPopupUI, ShopItemUI, ShopSystem |
| `int ItemCount` | ShopSystem |
| `int Price` | BuyConfirmationPopupUI, ShopItemUI, ShopSystem |
| `string EntryName` | BuyConfirmationPopupUI, ShopItemUI, ShopSystem |

### `Game/09_Shop/ConversionRule.cs`

| Member | Used by |
|---|---|
| `CharacterData ConversionOffer.Character` | ShopSystem |
| `ConversionRule ConversionOffer.Rule` | ShopSystem |
| `ItemData ConversionOffer.InputItem` | BuyConfirmationPopupUI, ShopComponentUI, ShopItemUI, ShopSystem |
| `RewardData ConversionOffer.Output` | BuyConfirmationPopupUI, ShopItemUI, ShopSystem |
| `bool ConversionOffer.IsValid` | ShopSystem |
| `int ConversionOffer.DailyLimit` | BuyConfirmationPopupUI, ShopItemUI, ShopSystem |
| `int ConversionOffer.InputAmount` | BuyConfirmationPopupUI, ShopItemUI, ShopSystem |
| `int ConversionOffer.OutputAmount` | ShopSystem |
| `string ConversionOffer.InputName` | BuyConfirmationPopupUI, ShopItemUI |
| `string ConversionOffer.Key` | ShopSystem |
| `string ConversionOffer.OutputName` | BuyConfirmationPopupUI, ShopItemUI, ShopSystem |
| `new ConversionOffer(ConversionRule rule, CharacterData character)` | ShopSystem |
| `ConversionKind ConversionRule.Kind` | ShopSystem |
| `RewardRarity ConversionRule.Rarity` | ShopSystem |

### `UI/Components/Shop/ShopComponentUI.cs`

_Scene/prefab component: no members called by other classes._

### `UI/Components/Shop/ShopItemUI.cs`

| Member | Used by |
|---|---|
| `void Setup(ShopItemEntry entry, Action buyCallback)` | ShopComponentUI |
| `void SetupConversion(ConversionOffer offer, int ownedInput, int remainingToday, Action convertCallback)` | ShopComponentUI |

### `UI/Components/Shop/BuyConfirmationPopup.cs`

| Member | Used by |
|---|---|
| `void Open(ShopData shop, ShopItemEntry entry)` | ShopComponentUI |
| `void OpenConversion(ConversionOffer offer)` | ShopComponentUI |

## Top-up (Bundle)

Real-money bundles through Midtrans (server: `payments/worker/src/index.js`).

### `Game/06_Bundle/BundleSystem.cs`

| Member | Used by |
|---|---|
| `string ServerUrl` | AccountSystem |
| `void BeginPurchase(BundleData bundleData, BundleEntry bundle, Action<PurchaseState, string> onUpdate)` | BundlePurchasePopupUI |
| `void ShowGrantedPopup(BundleEntry entry)` | BundlePurchasePopupUI |

### `Game/06_Bundle/BundleData.cs`

| Member | Used by |
|---|---|
| `BundleEntry[] Bundles` | BundleComponentUI, BundleSystem |
| `string BundleCategory` | BundleComponentUI |

### `Game/06_Bundle/BundleEntry.cs`

| Member | Used by |
|---|---|
| `BundleReward[] Rewards` | BundleSystem |
| `Sprite Icon` | BundleItemUI, BundlePurchasePopupUI |
| `float Price` | BundleItemUI, BundlePurchasePopupUI, BundleSystem |
| `string BundleName` | BundleItemUI, BundlePurchasePopupUI, BundleSystem |
| `string Description` | BundleItemUI, BundlePurchasePopupUI |
| `string ProductId` | BundleSystem |

### `Game/06_Bundle/BundleReward.cs`

| Member | Used by |
|---|---|
| `RewardData Reward` | BundleSystem |
| `int Amount` | BundleSystem |

### `Game/06_Bundle/MidtransBridge.cs`

| Member | Used by |
|---|---|
| `static MidtransBridge Instance` | BundleSystem |
| `static void Preload(string clientKey, bool isProduction)` | BundleSystem |
| `void Pay(string token, string redirectUrl, Action<string> onResult)` | BundleSystem |

### `UI/Components/Bundle/BundleComponentUI.cs`

_Scene/prefab component: no members called by other classes._

### `UI/Components/Bundle/BundleItemUI.cs`

| Member | Used by |
|---|---|
| `static string FormatPrice(float price)` | BundlePurchasePopupUI |
| `void Setup(BundleEntry entry, Action purchaseCallback)` | BundleComponentUI |

### `UI/Components/Bundle/BundlePurchasePopupUI.cs`

| Member | Used by |
|---|---|
| `void Open(BundleData data, BundleEntry entry)` | BundleComponentUI |

## Account / Cloud Save

Email-code login and cloud saves (server: `payments/worker/src/account.js`).

### `Game/13_Account/AccountSystem.cs`

| Member | Used by |
|---|---|
| `event Action AccountSystem.OnAccountChanged` | AccountSettingsRow |
| `event Action AccountSystem.OnSyncChanged` | AccountSettingsRow |
| `CloudSaveSummary AccountSystem.PendingConflict` | AccountMenuPrompt, AccountPopupUI |
| `DateTime? AccountSystem.LastSyncedAt` | AccountPopupUI |
| `bool AccountSystem.GuestChosen` | AccountMenuPrompt |
| `bool AccountSystem.HasServer` | AccountMenuPrompt, AccountSettingsRow |
| `bool AccountSystem.HasUnsyncedChanges` | AccountPopupUI |
| `bool AccountSystem.IsLoggedIn` | AccountMenuPrompt, AccountPopupUI, AccountSettingsRow, BundleSystem |
| `bool AccountSystem.IsUploading` | AccountPopupUI |
| `string AccountSystem.AccountId` | BundleSystem |
| `string AccountSystem.Email` | AccountPopupUI, AccountSettingsRow |
| `CloudSaveSummary AccountSystem.DeviceSummary()` | AccountPopupUI |
| `void AccountSystem.ChooseGuest()` | AccountPopupUI |
| `void AccountSystem.Logout()` | AccountPopupUI |
| `void AccountSystem.RequestCode(string email, Action<bool, string, string> done)` | AccountPopupUI |
| `void AccountSystem.ResolveConflict(bool useCloud, Action<bool, string> done = null)` | AccountPopupUI |
| `void AccountSystem.SyncNow(Action<bool, string> done = null)` | AccountPopupUI |
| `void AccountSystem.VerifyCode(string email, string code, Action<LoginOutcome, string> done)` | AccountPopupUI |
| `new CloudSaveSummary()` | AccountPopupUI |
| `string CloudSaveSummary.Describe()` | AccountPopupUI |

### `UI/Components/Account/AccountPopupUI.cs`

| Member | Used by |
|---|---|
| `static bool IsOpen` | AccountMenuPrompt |
| `static AccountPopupUI Show(bool firstLaunch = false)` | AccountMenuPrompt, AccountSettingsRow |

### `UI/Components/Account/AccountUIInstaller.cs`

_Scene/prefab component: no members called by other classes._

## Objectives

Daily, weekly, monthly and achievement objectives.

### `Game/12_Objective/ObjectiveSystem.cs`

| Member | Used by |
|---|---|
| `event Action ObjectiveSystem.OnObjectiveClaimed` | StorySystem |
| `event Action ObjectiveSystem.OnObjectivesChanged` | ObjectiveBadgeUI, ObjectiveMenuUI |
| `int ObjectiveSystem.TotalClaimableCount` | ObjectiveBadgeUI |
| `IEnumerable<ObjectiveDefinition> ObjectiveSystem.GetObjectives(ObjectivePeriod period)` | ObjectiveMenuUI |
| `TimeSpan ObjectiveSystem.TimeUntilReset(ObjectivePeriod period)` | ObjectiveMenuUI |
| `bool ObjectiveSystem.CanClaim(ObjectiveDefinition def)` | ObjectiveMenuUI |
| `bool ObjectiveSystem.Claim(ObjectiveDefinition def, bool showPopup = true)` | ObjectiveMenuUI |
| `bool ObjectiveSystem.IsClaimed(ObjectiveDefinition def)` | ObjectiveMenuUI |
| `bool ObjectiveSystem.RefreshStateObjectives()` | ObjectiveBadgeUI, ObjectiveMenuUI |
| `int ObjectiveSystem.ClaimAll(ObjectivePeriod period)` | ObjectiveMenuUI |
| `int ObjectiveSystem.GetClaimableCount(ObjectivePeriod period)` | ObjectiveMenuUI |
| `int ObjectiveSystem.GetProgress(ObjectiveDefinition def)` | ObjectiveMenuUI |
| `static void Objectives.Report(ObjectiveType type, int amount = 1)` | CurrencySystem, EnemyLifecycle, EnergySystem, GameplayManager, PlayerSystem +2 more |

### `Game/12_Objective/ObjectiveDatabase.cs`

| Member | Used by |
|---|---|
| `ObjectiveDefinition[] ObjectiveDatabase.Objectives` | ObjectiveSystem |
| `ObjectivePeriod ObjectiveDefinition.period` | ObjectiveSystem |
| `ObjectiveType ObjectiveDefinition.type` | ObjectiveSystem |
| `StoryRewardEntry[] ObjectiveDefinition.rewards` | ObjectiveRowUI, ObjectiveSystem |
| `bool ObjectiveDefinition.IsStateBased` | ObjectiveSystem |
| `int ObjectiveDefinition.target` | ObjectiveRowUI, ObjectiveSystem |
| `string ObjectiveDefinition.description` | ObjectiveRowUI |
| `string ObjectiveDefinition.id` | ObjectiveSystem |
| `string ObjectiveDefinition.targetId` | ObjectiveSystem |

### `UI/Components/Objective/ObjectiveMenuUI.cs`

_Scene/prefab component: no members called by other classes._

### `UI/Components/Objective/ObjectiveRowUI.cs`

| Member | Used by |
|---|---|
| `void Setup(ObjectiveDefinition def, int progress, bool claimed, Action<ObjectiveDefinition> claimCallback)` | ObjectiveMenuUI |

### `UI/Components/Objective/ObjectiveBadgeUI.cs`

| Member | Used by |
|---|---|
| `GameObject Badge` | StoryBadgeUI |

## Characters (data)

Character definitions and upgrade levels.

### `Game/02_Characters/Data/CharacterData.cs`

| Member | Used by |
|---|---|
| `CharacterRole CharacterData.CharacterRole` | Character, CharacterDetailPanelUI, CharacterListItemUI, GachaRevealUI |
| `CharacterType CharacterData.CharacterType` | Character, CharacterDetailPanelUI, CharacterListItemUI, GachaRevealUI, HierarchyGuide +1 more |
| `GameObject CharacterData.CharacterPrefab` | CharacterDetailPanelUI, GameplayInitializer, LevelSystem, TeamSelectionPopupUI |
| `ItemData CharacterData.DuplicateDust` | ConversionOffer, GachaSystem, RewardSystem |
| `ItemData CharacterData.DuplicateEngram` | ConversionOffer, GachaSystem, RewardSystem |
| `ItemData CharacterData.DustMaterial` | ConversionOffer |
| `ItemData CharacterData.EngramMaterial` | ConversionOffer |
| `Sprite CharacterData.CharacterPortrait` | Character, CharacterDetailPanelUI, CharacterListItemUI, GachaComponentUI, GachaRevealUI +5 more |
| `float CharacterData.BulletSpeed` | Character |
| `float CharacterData.ExSkillCooldown` | Character, CharacterDetailPanelUI |
| `float CharacterData.MovementSpeed` | Character |
| `int CharacterData.DuplicateDustAmount` | GachaSystem, RewardSystem |
| `int CharacterData.DuplicateEngramAmount` | GachaSystem, RewardSystem |
| `string CharacterData.Alias` | GachaRevealUI |
| `string CharacterData.CharacterId` | Character, CharacterDatabase, CharacterMenuUI, CharacterTapDialogue, ConversionOffer +6 more |
| `string CharacterData.CharacterName` | AnnihilatorManifestation, Character, CharacterDetailPanelUI, CharacterListItemUI, CharacterTapDialogue +18 more |
| `string CharacterData.Description` | GachaRevealUI |
| `CharacterUpgradeCost CharacterData.GetUpgradeCost(int currentLevel)` | PlayerSystem |
| `float CharacterData.GetAttackAtLevel(int level)` | Character, CharacterDetailPanelUI |
| `float CharacterData.GetDefenseAtLevel(int level)` | Character, CharacterDetailPanelUI |
| `float CharacterData.GetHealthAtLevel(int level)` | Character, CharacterDetailPanelUI |
| `ItemData CharacterUpgradeCost.Dust` | CharacterDetailPanelUI, PlayerSystem |
| `ItemData CharacterUpgradeCost.Engram` | CharacterDetailPanelUI, PlayerSystem |
| `int CharacterUpgradeCost.Coins` | CharacterDetailPanelUI, PlayerSystem |
| `int CharacterUpgradeCost.DustAmount` | CharacterDetailPanelUI, PlayerSystem |
| `int CharacterUpgradeCost.EngramAmount` | CharacterDetailPanelUI, PlayerSystem |

### `Game/02_Characters/Data/CharacterDatabase.cs`

| Member | Used by |
|---|---|
| `CharacterData[] Characters` | ObjectiveSystem, ProfileSummaryUI, ShopSystem, ShowcasePickerUI |
| `CharacterData FindById(string characterId)` | PlayerSystem |

### `Game/02_Characters/Data/CharacterProgression.cs`

| Member | Used by |
|---|---|
| `int AffectionLevel` | CharacterDetailPanelUI |
| `int AffectionPoints` | PlayerSystem |
| `int CurrentAffection` | CharacterDetailPanelUI |
| `int Level` | CharacterDetailPanelUI, CharacterListItemUI, PlayerSystem, TeamCharacterItemUI |
| `int RequiredAffection` | CharacterDetailPanelUI |
| `new CharacterProgression()` | PlayerSystem |
| `new CharacterProgression(int level, int affection)` | PlayerSystem |
| `void AddAffection(int amount)` | PlayerSystem |
| `void LevelUp()` | PlayerSystem |

### `Game/02_Characters/Enums/CharacterRole.cs`

_Scene/prefab component: no members called by other classes._

### `Game/02_Characters/Enums/CharacterType.cs`

_Scene/prefab component: no members called by other classes._

### `Game/02_Characters/Animator/IdleSpriteAnimator.cs`

_Scene/prefab component: no members called by other classes._

### `UI/Components/Character/CharacterMenuUI.cs`

_Scene/prefab component: no members called by other classes._

### `UI/Components/Character/CharacterListItemUI.cs`

| Member | Used by |
|---|---|
| `void CharacterListItemUI.Setup(CharacterData data, CharacterProgression progression, Action detailsCallback)` | CharacterMenuUI |
| `CharacterRole RoleSpriteMapping.role` | CharacterStatPanel |
| `Sprite RoleSpriteMapping.sprite` | CharacterStatPanel |
| `CharacterType TypeSpriteMapping.type` | CharacterStatPanel |
| `Sprite TypeSpriteMapping.sprite` | CharacterStatPanel |

### `UI/Components/Character/CharacterDetailPanelUI.cs`

| Member | Used by |
|---|---|
| `void SetNavigationCallbacks(Action previousCallback, Action nextCallback)` | CharacterMenuUI |
| `void SetUpgradeCallback(Action callback)` | CharacterMenuUI |
| `void ShowCharacter(CharacterData character, CharacterProgression progression, bool canNavigate, CharacterUpgradeCost upgradeCost, bool canUpgrade)` | CharacterMenuUI |

## Stage Select / Level

Stage list, team selection, sweep.

### `Game/07_Level/LevelSystem.cs`

| Member | Used by |
|---|---|
| `IReadOnlyList<CharacterData> CurrentTeam` | GameplayInitializer |
| `LevelData CurrentLevel` | EnemyDataBinder, GameplayInitializer, GameplayManager |
| `LevelPartData[] CurrentLevelParts` | EnemyWaveManager, LevelAreaController |
| `bool HasActiveSession` | GameplayInitializer |
| `string CurrentLevelName` | LevelAreaController |
| `bool PrepareLevel(LevelData level, IReadOnlyList<CharacterData> team)` | StorySystem, TeamSelectionPopupUI |
| `void ClearSession()` | StorySystem |

### `Game/07_Level/LevelData.cs`

| Member | Used by |
|---|---|
| `Color EnemyTint` | EnemyDataBinder |
| `LevelData PreviousLevel` | PlayerSystem |
| `LevelPartData[] LevelParts` | GameplayInitializer, LevelEnemyIntel, LevelSystem |
| `MusicReference LevelMusic` | LevelSystem |
| `RewardData[] LevelRewards` | GameplayInitializer, GameplayManager, LevelListItemUI, LevelSystem, StageSweep |
| `StoryRewardEntry[] RewardEntries` | GameplayManager, LevelListItemUI, StageSweep, StageSweepPopupUI |
| `StoryToastLine[] BattleToasts` | GameplayManager |
| `StoryToastLine[] BossIntroToasts` | GameplayManager |
| `float EnemyStatMultiplier` | EnemyDataBinder, LevelEnemyIntel |
| `int EnergyCost` | LevelListItemUI, LevelSystem, StageSweep, StageSweepPopupUI, TeamSelectionPopupUI |
| `int ExpReward` | PlayerSystem, StageSweepPopupUI |
| `int TimeLimitSeconds` | LevelTimerUI |
| `string LevelId` | GameplayManager, LevelSelectionUI, LevelSystem, PlayerSystem, StageSweep +1 more |
| `string LevelName` | GameplayInitializer, GameplayManager, LevelEnemyIntel, LevelListItemUI, LevelSelectionUI +3 more |

### `Game/07_Level/LevelPartData.cs`

| Member | Used by |
|---|---|
| `GameObject AreaPrefab` | LevelAreaController |
| `WaveData[] EnemyWaves` | EnemyWaveManager, LevelEnemyIntel |
| `string PartName` | EnemyWaveManager, LevelAreaController |

### `Game/07_Level/WaveData.cs`

| Member | Used by |
|---|---|
| `GameObject EnemyCollectionPrefab` | EnemyWaveManager, LevelEnemyIntel |
| `string WaveName` | EnemyWaveManager |

### `Game/07_Level/StageSweep.cs`

| Member | Used by |
|---|---|
| `static bool IsSweepable(LevelData level)` | TeamSelectionPopupUI |
| `static bool Sweep(LevelData level, int count, out string error)` | StageSweepPopupUI |
| `static int MaxAffordable(LevelData level)` | StageSweepPopupUI |

### `UI/Components/Stage/LevelSelectionUI.cs`

| Member | Used by |
|---|---|
| `event Action<LevelData, IReadOnlyList<CharacterData>> OnLevelStartRequested` | StageSelectTutorial |

### `UI/Components/Stage/LevelListItemUI.cs`

| Member | Used by |
|---|---|
| `bool IsCompleted` | StageSelectTutorial |
| `bool IsUnlocked` | StageSelectTutorial |
| `void Setup(LevelData data, bool isUnlocked, bool isCompleted, Action<LevelData> startCallback)` | LevelSelectionUI |

### `UI/Components/Stage/TeamSelectionPopupUI.cs`

| Member | Used by |
|---|---|
| `void Show(LevelData level, Action<LevelData, IReadOnlyList<CharacterData>> startCallback)` | LevelSelectionUI |

### `UI/Components/Stage/TeamCharacterItemUI.cs`

| Member | Used by |
|---|---|
| `void Setup(CharacterData character, CharacterProgression progression, bool selected, Action<CharacterData> callback)` | TeamSelectionPopupUI |

### `UI/Components/Stage/StageSweepPopupUI.cs`

| Member | Used by |
|---|---|
| `static StageSweepPopupUI Show(LevelData level, Action onSwept = null)` | TeamSelectionPopupUI |

### `UI/Components/Stage/StageSelectTutorial.cs`

| Member | Used by |
|---|---|
| `static bool IsDone` | OnboardingLauncher |

## Story

Chapters, dialogue, story battles, onboarding, stage gates.

### `Game/11_Story/StorySystem.cs`

| Member | Used by |
|---|---|
| `event Action OnDialogueSegmentReady` | StorySceneController |
| `CharacterData PlayerAvatar` | AnnihilatorManifestation |
| `IReadOnlyList<StoryData> OnboardingChapters` | StoryProgress |
| `StoryData CurrentStory` | StorySceneController |
| `StoryData FollowUpStory` | StorySceneController |
| `StoryData NextOnboardingChapter` | OnboardingLauncher |
| `StorySegment CurrentSegment` | StoryBattleController, StorySceneController |
| `StoryToastUI Toasts` | AnnihilatorManifestation, CharacterTapDialogue, ExSkillController, GameplayManager, ObjectiveMenuUI +3 more |
| `bool IsBusy` | OnboardingLauncher |
| `bool IsInStoryBattle` | GameplayManager |
| `int SegmentIndex` | StorySceneController |
| `CharacterData ResolveSpeaker(CharacterData speaker, string speakerName)` | StoryDialogueUI, StoryToastUI |
| `void CompleteDialogueSegment()` | StorySceneController |
| `void CompleteFollowUp()` | StorySceneController |
| `void ReportBattleResult(bool won)` | StoryBattleController |
| `void StartStory(StoryData story, bool onboarding)` | OnboardingLauncher, StorySelectionUI |

### `Game/11_Story/StoryData.cs`

| Member | Used by |
|---|---|
| `GameObject StoryPrefab` | StorySceneController |
| `LevelData RequiredLevel` | StoryProgress |
| `MusicReference StoryMusic` | StorySceneController |
| `RewardData[] StoryRewards` | StorySystem |
| `SceneReference AfterScene` | StorySystem |
| `Sprite Background` | StoryDialogueUI |
| `StoryAfterAction AfterAction` | StorySystem |
| `StoryData NextStory` | StoryProgress, StorySystem |
| `StoryData PreviousStory` | StoryProgress |
| `StoryLine[] FollowUpLines` | StorySceneController |
| `StoryRewardEntry[] RewardEntries` | StorySystem |
| `StorySegment[] Segments` | StorySystem |
| `StoryToastLine[] AfterActionToasts` | StorySystem |
| `StoryToastLine[] AfterGachaPullToasts` | StorySystem |
| `StoryToastLine[] AfterSceneToasts` | StorySystem |
| `bool HasFollowUp` | StorySystem |
| `bool HasSegments` | StorySceneController, StorySystem |
| `string ChapterLabel` | StoryDialogueUI, StoryEndPopupUI, StoryListItemUI |
| `string StoryDescription` | StoryConfirmPopupUI |
| `string StoryId` | StoryProgress, StorySelectionUI, StorySystem |
| `string StoryName` | StoryConfirmPopupUI, StoryDialogueUI, StoryEndPopupUI, StoryListItemUI, StorySceneController +2 more |

### `Game/11_Story/StoryTypes.cs`

| Member | Used by |
|---|---|
| `CharacterData StoryLine.speaker` | StoryDialogueUI |
| `Sprite StoryLine.background` | StoryDialogueUI |
| `StoryLineEffect StoryLine.effect` | StoryDialogueUI |
| `StoryLineStyle StoryLine.style` | StoryDialogueUI |
| `bool StoryLine.blackScreen` | StoryDialogueUI |
| `bool StoryLine.rightSide` | StoryDialogueUI |
| `string StoryLine.DisplayName` | StoryDialogueUI |
| `string StoryLine.heading` | StoryDialogueUI |
| `string StoryLine.label` | StoryDialogueUI |
| `string StoryLine.speakerName` | StoryDialogueUI |
| `string StoryLine.text` | StoryDialogueUI |
| `RewardData StoryRewardEntry.reward` | GameplayManager, LevelListItemUI, ObjectiveRowUI, ObjectiveSystem, StageSweep +2 more |
| `int StoryRewardEntry.amount` | GameplayManager, ObjectiveRowUI, ObjectiveSystem, StageSweep, StageSweepPopupUI +1 more |
| `CharacterData[] StorySegment.team` | StorySystem |
| `LevelData StorySegment.level` | StorySystem |
| `StoryLine[] StorySegment.lines` | StorySceneController |
| `StorySegmentType StorySegment.type` | StorySceneController, StorySystem |
| `StoryToastLine[] StorySegment.battleToasts` | StoryBattleController |
| `bool StorySegment.scriptedLoss` | StoryBattleController |
| `float StorySegment.scriptedLossSeconds` | StoryBattleController |
| `CharacterData StoryToastLine.speaker` | AnnihilatorManifestation, CharacterTapDialogue, ExSkillController, ObjectiveMenuUI, OnboardingLauncher +2 more |
| `StoryToastTrigger StoryToastLine.trigger` | StoryBattleController |
| `float StoryToastLine.duration` | AnnihilatorManifestation, CharacterTapDialogue, ExSkillController, ObjectiveMenuUI, OnboardingLauncher +2 more |
| `float StoryToastLine.notBefore` | StoryBattleController |
| `float StoryToastLine.time` | GameplayManager, StageSelectTutorial, StoryBattleController, StoryToastUI |
| `string StoryToastLine.DisplayName` | StoryToastUI |
| `string StoryToastLine.speakerName` | AnnihilatorManifestation, CharacterTapDialogue, ExSkillController, StoryToastUI |
| `string StoryToastLine.text` | AnnihilatorManifestation, CharacterTapDialogue, ExSkillController, ObjectiveMenuUI, OnboardingLauncher +2 more |
| `new StoryToastLine()` | AnnihilatorManifestation, CharacterTapDialogue, ExSkillController, ObjectiveMenuUI, OnboardingLauncher +1 more |

### `Game/11_Story/StoryProgress.cs`

| Member | Used by |
|---|---|
| `static bool IsNew(StoryData story, PlayerSystem player)` | StorySelectionUI |
| `static bool IsUnlocked(StoryData story, PlayerSystem player)` | StorySelectionUI, StorySystem |
| `static int NewCount(PlayerSystem player)` | StoryBadgeUI |
| `static string LockReason(StoryData story, PlayerSystem player)` | StorySelectionUI |

### `Game/11_Story/StorySession.cs`

| Member | Used by |
|---|---|
| `static StoryData Selected` | StorySceneController, StorySelectionUI, StorySystem |
| `static void Clear()` | StorySystem |

### `Game/11_Story/StorySceneController.cs`

_Scene/prefab component: no members called by other classes._

### `Game/11_Story/StoryBattleController.cs`

| Member | Used by |
|---|---|
| `static StoryBattleController Active` | GameOverTrigger, GameplayManager |
| `bool HandleDefeat()` | GameOverTrigger |
| `void HandleVictory()` | GameplayManager |

### `UI/Components/Story/StoryDialogueUI.cs`

| Member | Used by |
|---|---|
| `void Play(StoryData story, StoryLine[] segmentLines, StoryData titleStory, Action finished)` | StorySceneController |

### `UI/Components/Story/StoryToastUI.cs`

| Member | Used by |
|---|---|
| `void HideAll()` | StageSelectTutorial, StoryBattleController, StorySystem |
| `void HideBanner()` | StoryBattleController |
| `void Interrupt(StoryToastLine line)` | StoryBattleController |
| `void PlaySequence(IEnumerable<StoryToastLine> lines)` | GameplayManager, StageSelectTutorial, StorySystem |
| `void Show(StoryToastLine line)` | CharacterTapDialogue, ExSkillController, GameplayManager, ObjectiveMenuUI, OnboardingLauncher +1 more |
| `void ShowBanner(string text, bool positive)` | StoryBattleController |
| `void ShowPriority(StoryToastLine line)` | AnnihilatorManifestation, StoryBattleController |

### `UI/Components/Story/StorySelectionUI.cs`

_Scene/prefab component: no members called by other classes._

### `UI/Components/Story/StoryListItemUI.cs`

| Member | Used by |
|---|---|
| `void Setup(StoryData data, bool isUnlocked, bool isCompleted, Action<StoryData> startCallback, bool isNew = false, string lockReason = null)` | StorySelectionUI |

### `UI/Components/Story/StoryConfirmPopupUI.cs`

| Member | Used by |
|---|---|
| `void Show(StoryData story, Action<StoryData> confirmCallback)` | StorySelectionUI |

### `UI/Components/Story/StoryBadgeUI.cs`

_Scene/prefab component: no members called by other classes._

### `UI/Components/Story/StoryEndPopupUI.cs`

| Member | Used by |
|---|---|
| `static StoryEndPopupUI Show(StoryData finished, StoryData next, Action<bool> onChoice)` | StorySystem |

### `UI/Components/Story/OnboardingLauncher.cs`

_Scene/prefab component: no members called by other classes._

### `UI/Components/Story/BossHealthBarUI.cs`

_Scene/prefab component: no members called by other classes._

## Character flavour

Tap dialogue and character quips in menus.

### `UI/Generic/CharacterQuips.cs`

| Member | Used by |
|---|---|
| `static string[] Get(string characterId, QuipContext context)` | CharacterTapDialogue |

### `UI/Generic/CharacterTapDialogue.cs`

_Scene/prefab component: no members called by other classes._


---

# Combat

## Battle flow

Starts a battle, handles win and loss, returns to the menu.

### `Game/03_Gameplay/System/GameplayManager.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/System/GameplayInitializer.cs`

| Member | Used by |
|---|---|
| `IReadOnlyList<Character> InitializedCharacters` | CharacterStatUI, CharacterSwitcher, ExSkillController, SkillTargets |
| `LevelData CurrentLevel` | LevelTimerUI |
| `bool IsInitialized` | CharacterSwitcher |
| `bool InitializeGameplay()` | CharacterSwitcher |

### `Game/03_Gameplay/System/GameOverTrigger.cs`

_Scene/prefab component: no members called by other classes._

### `UI/HUD/GameOver/GameOverHUD.cs`

_Scene/prefab component: no members called by other classes._

### `UI/HUD/LevelTimer/LevelTimerUI.cs`

_Scene/prefab component: no members called by other classes._

### `UI/HUD/Pause/GameplayPauseMenu.cs`

_Scene/prefab component: no members called by other classes._

## Play area / Camera

Fits the camera to the play area and scrolls the background.

### `Game/03_Gameplay/System/PlayAreaCameraFit.cs`

| Member | Used by |
|---|---|
| `Rect PlayArea` | AnnihilatorManifestation |
| `static PlayAreaCameraFit Ensure()` | GameplayManager |

### `Game/03_Gameplay/Area/AreaLoopController.cs`

| Member | Used by |
|---|---|
| `event Action<int, Vector3> AreaRecycled` | LevelAreaController |
| `static float CameraBottomY(Camera cam)` | LevelAreaController |
| `static float CameraTopY(Camera cam)` | LevelAreaController |
| `void Initialize(Transform[] tiles, float height, float speed, float recyclePositionY)` | LevelAreaController |
| `void ReplaceArea(int index, Transform replacement)` | LevelAreaController |

### `Game/03_Gameplay/Area/LevelAreaController.cs`

_Scene/prefab component: no members called by other classes._

## Characters (in battle)

Stats, movement and auto-fire of party members.

### `Game/02_Characters/Character.cs`

| Member | Used by |
|---|---|
| `CharacterData Data` | ExSkillController |
| `CharacterRole CharacterRole` | AnonCharacter, BokusuCharacter, CashinoCharacter, CharacterStatPanel, EiPiCharacter +8 more |
| `CharacterType CharacterType` | AnonCharacter, BokusuCharacter, BulletSpawner, CashinoCharacter, CharacterStatPanel +12 more |
| `Sprite CharacterIcon` | CharacterStatPanel |
| `float AttackPower` | AllOrNothingExSkill, AnonCharacter, BokusuCharacter, CashinoCharacter, EiPiCharacter +13 more |
| `float BulletSpeed` | AnonCharacter, BokusuCharacter, CashinoCharacter, EiPiCharacter, EyayCharacter +9 more |
| `float DefensePower` | AnonCharacter, BokusuCharacter, CashinoCharacter, EiPiCharacter, EmergencyRegulationExSkill +10 more |
| `float EffectiveAttackPower` | ArcaneImpossibilityExSkill, PerfectAnnotationExSkill, PlayerAutoShooter, SignalOverloadExSkill, SwitchRewards +1 more |
| `float EffectiveDefensePower` | AbsoluteContainmentExSkill, FairExchangeExSkill, UnknownOutcomeExSkill |
| `float ExSkillCooldown` | AnonCharacter, BokusuCharacter, CashinoCharacter, EiPiCharacter, ExSkill +9 more |
| `float FireIntervalMultiplier` | PlayerAutoShooter |
| `float Health` | AnonCharacter, BokusuCharacter, CashinoCharacter, EiPiCharacter, EyayCharacter +7 more |
| `float MovementSpeed` | AnonCharacter, BokusuCharacter, CashinoCharacter, CharacterCombatController, EiPiCharacter +8 more |
| `string CharacterId` | AnonCharacter, BokusuCharacter, CashinoCharacter, EiPiCharacter, EyayCharacter +7 more |
| `string CharacterName` | AllOrNothingExSkill, AnonCharacter, BokusuCharacter, CashinoCharacter, CharacterStatPanel +11 more |
| `void Initialize(CharacterData data)` | GameplayInitializer |
| `void RemoveModifiers(object source)` | ExSkill, SwitchRewards |
| `void SetAttackModifier(object source, float flatAmount)` | ExSkill, SwitchRewards |
| `void SetDefenseModifier(object source, float flatAmount)` | ExSkill |
| `void SetFireIntervalModifier(object source, float intervalMultiplier)` | ExSkill, SwitchRewards |

### `Game/02_Characters/Unit/Attacker/EiPiCharacter.cs`

_Scene/prefab component: no members called by other classes._

### `Game/02_Characters/Unit/Attacker/MajikCharacter.cs`

_Scene/prefab component: no members called by other classes._

### `Game/02_Characters/Unit/Attacker/WarlockCharacter.cs`

_Scene/prefab component: no members called by other classes._

### `Game/02_Characters/Unit/Executor/TrenCharacter.cs`

_Scene/prefab component: no members called by other classes._

### `Game/02_Characters/Unit/Executor/VioCharacter.cs`

_Scene/prefab component: no members called by other classes._

### `Game/02_Characters/Unit/Manipulator/CashinoCharacter.cs`

_Scene/prefab component: no members called by other classes._

### `Game/02_Characters/Unit/Pacifist/TradyCharacter.cs`

_Scene/prefab component: no members called by other classes._

### `Game/02_Characters/Unit/Protector/AnonCharacter.cs`

_Scene/prefab component: no members called by other classes._

### `Game/02_Characters/Unit/Protector/BokusuCharacter.cs`

_Scene/prefab component: no members called by other classes._

### `Game/02_Characters/Unit/Protector/ReguCharacter.cs`

_Scene/prefab component: no members called by other classes._

### `Game/02_Characters/Unit/Support/EyayCharacter.cs`

_Scene/prefab component: no members called by other classes._

### `Game/02_Characters/Unit/Support/NozCharacter.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Player/PlayerMovement.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Player/PlayerAutoShooter.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Player/GameplayHotkeys.cs`

_Scene/prefab component: no members called by other classes._

## Party Switching

Switching window, defeat and revive, switch rewards.

### `Game/03_Gameplay/Switcher/CharacterSwitcher.cs`

| Member | Used by |
|---|---|
| `static event Action ManualSwitchStarted` | StoryBattleController |
| `static event Action<Character> FieldCharacterChanged` | ExSkill |
| `Character ActiveCharacterComponent` | EnemyTarget, ExSkillController |
| `GameObject ActiveCharacter` | AnnihilatorManifestation, PlayerAutoShooter, StoryBattleController |
| `SwitchRewards Rewards` | StoryBattleController |
| `bool CanSwitch` | StoryBattleController |
| `bool IsSwitching` | AnnihilatorManifestation, ExSkillController, PlayerAutoShooter, StoryBattleController |
| `float CooldownRemaining` | SwitchCooldownUI |
| `float SwitchCooldownDuration` | SwitchCooldownUI |
| `float GetEffectiveMovementSpeed()` | PlayerMovement |
| `void RefundCooldown(float fraction)` | SwitchRewards |
| `void ResetCooldown()` | StoryBattleController |
| `void SwitchTo(int targetIndex)` | GameplayHotkeys |

### `Game/03_Gameplay/Switcher/CharacterSwitcher.Setup.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Switcher/CharacterSwitcher.Switching.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Switcher/CharacterSwitcher.Defeat.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Switcher/CharacterSwitcher.Feel.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Switcher/CharacterSwitchAnimation.cs`

| Member | Used by |
|---|---|
| `IEnumerator Animate(GameObject[] characters, bool[] defeatedCharacters, Vector3[] targets, float duration, Func<int, Vector3, Vector3> clampPosition)` | CharacterSwitcher |

### `Game/03_Gameplay/Switcher/CharacterLifeController.cs`

| Member | Used by |
|---|---|
| `event Action<int> CharacterDefeated` | CharacterSwitcher, GameOverTrigger |
| `event Action<int> CharacterRevived` | CharacterSwitcher |
| `bool AllCharactersDefeated` | GameOverTrigger |
| `bool IsInitialized` | GameOverTrigger |
| `int CharacterCount` | AllOrNothingExSkill |
| `bool CanRevive(int index)` | AllOrNothingExSkill |
| `bool HasLivingCharacterOtherThan(int currentIndex)` | CharacterSwitcher |
| `bool IsAlive(int index)` | CharacterSwitcher |
| `bool Revive(int index)` | AllOrNothingExSkill |
| `int FindNextLivingCharacter(int currentIndex)` | CharacterSwitcher |
| `void Initialize(GameObject[] characterObjects, Health[] characterHealths, bool[] defeatedCharacters)` | CharacterSwitcher |

### `Game/03_Gameplay/Switcher/CharacterCombatController.cs`

| Member | Used by |
|---|---|
| `float GetAverageMovementSpeed(int activeCharacterIndex, bool isSwitching, Health[] healths)` | CharacterSwitcher |
| `void Initialize(GameObject[] characterObjects, bool[] defeated)` | CharacterSwitcher |
| `void RefreshAutoShoot(int activeCharacterIndex, bool isSwitching)` | CharacterSwitcher |

### `Game/03_Gameplay/Switcher/CharacterBoundaryController.cs`

| Member | Used by |
|---|---|
| `Vector3 ClampChildLocalPosition(BoxCollider2D childCollider, Transform childTransform, Vector3 desiredLocalPosition)` | CharacterSwitcher |
| `bool Initialize(BoxCollider2D arenaCollider)` | CharacterSwitcher |

### `Game/03_Gameplay/Switcher/SwitchRewards.cs`

| Member | Used by |
|---|---|
| `static event Action CounterTagInPerformed` | StoryBattleController |
| `static event Action PerfectSwitchPerformed` | StoryBattleController |
| `void OnForcedSwap()` | CharacterSwitcher |
| `void OnManualSwitchCompleted(Character incoming)` | CharacterSwitcher |
| `void OnManualSwitchStarted(Character outgoing)` | CharacterSwitcher |
| `void OpenPerfectWindow(float seconds)` | StoryBattleController |

### `UI/HUD/SwitchIndicator/SwitchCooldownUI.cs`

_Scene/prefab component: no members called by other classes._

## EX Skills

The 12 character skills, targeting, carry-over between units.

### `Game/03_Gameplay/Projectile/ExSkill/ExSkill.cs`

| Member | Used by |
|---|---|
| `Character Owner` | AbsoluteContainmentExSkill, AllOrNothingExSkill, ArcaneImpossibilityExSkill, EmergencyRegulationExSkill, FairExchangeExSkill +7 more |
| `Color SkillColor` | AbsoluteContainmentExSkill, ArcaneImpossibilityExSkill, EmergencyRegulationExSkill, ExSkillController, FairExchangeExSkill +7 more |
| `SFXReference ActivationSfx` | ExSkillController |
| `bool PlayDefaultActivationFeel` | ExSkillController |
| `string SkillName` | CharacterDetailPanelUI, ExSkillController |
| `bool CanUse()` | AbsoluteContainmentExSkill, AllOrNothingExSkill, ArcaneImpossibilityExSkill, EmergencyRegulationExSkill, ExSkillController +8 more |
| `string Describe(float attack, float defense)` | CharacterDetailPanelUI |
| `string PickActivationLine()` | ExSkillController |
| `void Activate()` | ExSkillController |
| `void Cancel()` | AbsoluteContainmentExSkill, AllOrNothingExSkill, ArcaneImpossibilityExSkill, EmergencyRegulationExSkill, FairExchangeExSkill +7 more |
| `void Initialize(Character character)` | Character, ExSkillController |

### `Game/03_Gameplay/Projectile/ExSkill/SkillTargets.cs`

| Member | Used by |
|---|---|
| `static IReadOnlyList<Character> Party` | AllOrNothingExSkill, ExSkill, PerfectAnnotationExSkill, RecursiveOptimizationExSkill |
| `static Health HighestHealthEnemy(bool onScreenOnly = true)` | TotalWarfareExSkill |
| `static Health NearestEnemy(Vector2 from, ICollection<Health> among = null)` | ArcaneImpossibilityExSkill |
| `static List<Health> AliveEnemies(bool onScreenOnly = true)` | AnnihilatorHierarchyChoiceUI, PerfectAnnotationExSkill, SwitchRewards |
| `static List<Health> EnemiesInRadius(Vector2 center, float radius, bool onScreenOnly = false)` | ArcaneImpossibilityExSkill, SignalOverloadExSkill, SwitchRewards, TotalWarfareExSkill |
| `static bool IsPartyMember(Character character)` | ArcaneImpossibilityExSkill, PerfectAnnotationExSkill, RecursiveOptimizationExSkill |
| `static void AddStatus(Health target, string effectId, float duration)` | ArcaneImpossibilityExSkill, PerfectAnnotationExSkill, SignalOverloadExSkill |
| `static void RemoveStatus(Health target, string effectId)` | PerfectAnnotationExSkill |

### `Game/03_Gameplay/Misc/ExSkillController.cs`

| Member | Used by |
|---|---|
| `Character CooldownCharacter` | CharacterStatPanel |
| `float CooldownDuration` | CharacterStatPanel |
| `float CooldownRemaining` | CharacterStatPanel |
| `void UseExSkill()` | GameplayHotkeys |

### `Game/03_Gameplay/Projectile/ExSkill/Characters/AbsoluteContainmentExSkill.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Projectile/ExSkill/Characters/AllOrNothingExSkill.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Projectile/ExSkill/Characters/ArcaneImpossibilityExSkill.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Projectile/ExSkill/Characters/EmergencyRegulationExSkill.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Projectile/ExSkill/Characters/FairExchangeExSkill.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Projectile/ExSkill/Characters/PerfectAnnotationExSkill.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Projectile/ExSkill/Characters/RecursiveOptimizationExSkill.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Projectile/ExSkill/Characters/SignalOverloadExSkill.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Projectile/ExSkill/Characters/TotalWarfareExSkill.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Projectile/ExSkill/Characters/UnknownOutcomeExSkill.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Projectile/ExSkill/Characters/UnrestrainedViolenceExSkill.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Projectile/ExSkill/Characters/ViralSensationExSkill.cs`

_Scene/prefab component: no members called by other classes._

## Annihilator

The player's own manifestation and its hierarchy choice.

### `Game/03_Gameplay/Annihilator/AnnihilatorManifestation.cs`

| Member | Used by |
|---|---|
| `static event Action HierarchyChoiceOpened` | StoryBattleController |
| `static event Action Manifested` | StoryBattleController |
| `static bool IsAnyManifesting` | CharacterSwitcher, ExSkillController |
| `string HotkeyLabel` | GameplayHotkeys |
| `void ActivateManifestation()` | GameplayHotkeys |

### `Game/03_Gameplay/Annihilator/AnnihilatorAutoShooter.cs`

| Member | Used by |
|---|---|
| `CharacterType? AttackerType` | AnnihilatorManifestation |
| `float AimAngle` | AnnihilatorManifestation |
| `float DamageOverride` | AnnihilatorManifestation |
| `float IntervalMultiplier` | AnnihilatorManifestation |
| `float SweepPhase` | AnnihilatorManifestation |

### `Game/03_Gameplay/Annihilator/AnnihilatorHierarchyChoiceUI.cs`

| Member | Used by |
|---|---|
| `static AnnihilatorHierarchyChoiceUI Show(TMP_FontAsset font, Sprite frame, CharacterType? recommended, Action<CharacterType> onPick, Action onCancel)` | AnnihilatorManifestation |
| `static CharacterType? RecommendedForField()` | AnnihilatorManifestation |
| `void Cancel()` | AnnihilatorManifestation |

## Enemies

Data-driven enemies, waves and bosses.

### `Game/03_Gameplay/Enemy/EnemyData.cs`

| Member | Used by |
|---|---|
| `CharacterType characterType` | AnnihilatorHierarchyChoiceUI, BulletSpawner, CounterMechanics, LevelEnemyIntel, Projectile +1 more |
| `EnemyFacingMode facingMode` | EnemyMovement |
| `EnemyMovementType movementType` | EnemyMovement |
| `EnemyPatternSlot[] patterns` | EnemyShooter |
| `EnemyPatternSlot[] phase2Patterns` | EnemyShooter |
| `Vector2 center` | EnemyMovement |
| `Vector2 targetPoint` | EnemyMovement |
| `bool alwaysAimAtPlayer` | EnemyShooter |
| `bool fireOnlyOnScreen` | EnemyShooter |
| `bool infiniteLifetime` | EnemyLifecycle |
| `bool mirrorX` | EnemyMovement |
| `float amplitudeX` | EnemyMovement |
| `float angularSpeed` | EnemyMovement |
| `float blinkInterval` | EnemyMovement |
| `float defensePower` | EnemyDataBinder |
| `float descendSpeed` | EnemyMovement |
| `float diveSpeed` | EnemyMovement |
| `float enterSpeed` | EnemyMovement |
| `float facingSpeed` | EnemyMovement |
| `float fadeTime` | EnemyMovement |
| `float frequency` | EnemyMovement |
| `float hitAndRunExitSpeed` | EnemyMovement |
| `float holdY` | EnemyMovement |
| `float idleTime` | EnemyMovement |
| `float lateralSpeed` | EnemyMovement |
| `float lifetime` | EnemyLifecycle |
| `float lifetimeExitSpeed` | EnemyLifecycle |
| `float maxHealth` | EnemyDataBinder, LevelEnemyIntel |
| `float moveDownSpeed` | EnemyMovement |
| `float moveToSpeed` | EnemyMovement |
| `float phase2Threshold` | EnemyShooter |
| `float projectileDamage` | EnemyDataBinder |
| `float projectileSpeed` | EnemyDataBinder |
| `float radius` | EnemyMovement |
| `float rangeX` | EnemyMovement |
| `float retreatSpeed` | EnemyMovement |
| `float speed` | EnemyMovement |
| `float spinSpeed` | EnemyMovement |
| `float stayTime` | EnemyMovement |
| `float switchInterval` | EnemyMovement |
| `int randomSeed` | EnemyShooter |

### `Game/03_Gameplay/Enemy/EnemyDataBinder.cs`

| Member | Used by |
|---|---|
| `EnemyData Data` | AnnihilatorHierarchyChoiceUI, BulletSpawner, CounterMechanics, LevelEnemyIntel, Projectile +1 more |

### `Game/03_Gameplay/Enemy/EnemyMovement.cs`

| Member | Used by |
|---|---|
| `event Action OnLeftScreen` | EnemyLifecycle |
| `bool SuppressFire` | EnemyShooter |
| `bool SuppressOwnExit` | EnemyLifecycle |
| `void ApplyData(EnemyData data)` | EnemyDataBinder |
| `void BeginExit(float speed)` | EnemyLifecycle |
| `void StopMoving()` | EnemyLifecycle |

### `Game/03_Gameplay/Enemy/EnemyShooter.cs`

| Member | Used by |
|---|---|
| `bool IsPhase2` | EnemyWaveManager |
| `void ApplyData(EnemyData data)` | EnemyDataBinder |
| `void StopShooting()` | EnemyLifecycle |

### `Game/03_Gameplay/Enemy/EnemyPatternSpawner.cs`

| Member | Used by |
|---|---|
| `bool CanSpawn` | EnemyShooter |
| `Projectile Spawn(Vector2 position, Vector2 direction, float speed, float scale = 1, float sineAmplitude = 0, float sineFrequency = 0)` | EnemyShooter |
| `void PlayFireSFX()` | EnemyShooter |

### `Game/03_Gameplay/Enemy/EnemyLifecycle.cs`

| Member | Used by |
|---|---|
| `static event Action<EnemyLifecycle> OnAnyEnemyDied` | CombatFeelRunner |
| `Health Health` | SkillTargets |
| `bool IsAlive` | SkillTargets |
| `static IReadOnlyList<EnemyLifecycle> Active` | SkillTargets |
| `void ApplyData(EnemyData data)` | EnemyDataBinder |

### `Game/03_Gameplay/Enemy/EnemyCollection.cs`

| Member | Used by |
|---|---|
| `bool IsCleared` | EnemyWaveManager |

### `Game/03_Gameplay/Enemy/EnemyWaveManager.cs`

| Member | Used by |
|---|---|
| `event Action OnLevelWon` | GameplayManager |
| `void StartLevelWaves()` | GameplayInitializer |

### `Game/03_Gameplay/Enemy/EnemyBoss.cs`

| Member | Used by |
|---|---|
| `static event Action<EnemyBoss> OnBossAppeared` | GameplayManager |
| `CharacterData Character` | BossHealthBarUI |
| `Color BarColor` | BossHealthBarUI |
| `Health Health` | BossHealthBarUI |
| `static IReadOnlyList<EnemyBoss> Active` | BossHealthBarUI |
| `string DisplayName` | BossHealthBarUI, EnemyWaveManager, LevelEnemyIntel |

### `Game/03_Gameplay/Enemy/EnemyTarget.cs`

| Member | Used by |
|---|---|
| `static Vector2 DirectionFrom(Vector2 origin, Vector2 fallbackDirection)` | EnemyMovement, EnemyShooter |
| `static Vector2 Position(Vector2 fallback)` | EnemyMovement |

### `Game/03_Gameplay/Enemy/EnemyHealthUI.cs`

_Scene/prefab component: no members called by other classes._

## Projectiles

Bullets and their spawn patterns.

### `Game/03_Gameplay/Projectile/Projectile.cs`

| Member | Used by |
|---|---|
| `static event Action<Character, Health, float, bool> OnHit` | TotalWarfareExSkill, UnrestrainedViolenceExSkill, ViralSensationExSkill |
| `bool IsHostileToPlayer` | AbsoluteContainmentExSkill, ProjectileLimiter, StoryBattleController, SwitchRewards, UnknownOutcomeExSkill |
| `float Damage` | AbsoluteContainmentExSkill |
| `new Projectile()` | HomingProjectile, SineProjectile |
| `void Initialize(Vector3 position, Quaternion rotation, float projectileSpeed, float projectileDamage, LayerMask targetMask)` | BulletSpawner |
| `void OnSpawn()` | HomingProjectile |
| `void ReturnToPool()` | AbsoluteContainmentExSkill, ProjectileLimiter, SwitchRewards, UnknownOutcomeExSkill |
| `void SetAttackerType(CharacterType? type)` | BulletSpawner |
| `void SetOwner(Character owner)` | BulletSpawner |
| `void SetReturnCallback(Action<Projectile> callback)` | BulletSpawner |

### `Game/03_Gameplay/Projectile/HomingProjectile.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Projectile/SineProjectile.cs`

| Member | Used by |
|---|---|
| `void SetSine(float newAmplitude, float newFrequency)` | EnemyPatternSpawner |

### `Game/03_Gameplay/Projectile/ProjectileLimiter.cs`

| Member | Used by |
|---|---|
| `Color? CullColor` | EmergencyRegulationExSkill |
| `int MaxEnemyProjectiles` | EmergencyRegulationExSkill |
| `void SetMaxEnemyProjectiles(int max)` | EmergencyRegulationExSkill |

### `Game/03_Gameplay/Projectile/Spawner/BulletSpawner.cs`

| Member | Used by |
|---|---|
| `Character OwnerCharacter` | PlayerAutoShooter |
| `CharacterType? AttackerTypeOverride` | AnnihilatorAutoShooter |
| `void ApplyProjectileStats(float damage, float speed)` | EnemyDataBinder |
| `void Fire(Transform firePoint, float speedOverride)` | BurstSpawner, CircularSpawner |
| `void Fire(Transform firePoint, float speedOverride, float damageOverride)` | AnnihilatorAutoShooter, PlayerAutoShooter, TotalWarfareExSkill |

### `Game/03_Gameplay/Projectile/Spawner/CircularSpawner.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Projectile/Spawner/BurstSpawner.cs`

_Scene/prefab component: no members called by other classes._

## Health / Damage

HP, shields and damage numbers.

### `Game/03_Gameplay/Generic/Health.cs`

| Member | Used by |
|---|---|
| `event Action OnDamaged` | EnemyHealthUI, SquashBlink |
| `event Action OnDeath` | CharacterLifeController, CharacterStatPanel, EnemyHealthUI, EnemyLifecycle |
| `event Action<float, float> OnHealthChanged` | CharacterStatPanel, EnemyHealthUI, EnemyShooter |
| `static event Action<Health, float> OnAnyDamaged` | CombatFeelRunner, DamagePopup |
| `static event Action<Health, float> OnAnyHealed` | DamagePopup |
| `Shield Shield` | AbsoluteContainmentExSkill, ExSkill, FairExchangeExSkill, UnknownOutcomeExSkill |
| `bool DeferDeathVFX` | EnemyLifecycle |
| `bool IsDead` | ArcaneImpossibilityExSkill, BossHealthBarUI, CharacterCombatController, CharacterLifeController, CharacterSwitcher +12 more |
| `float CurrentHealth` | BossHealthBarUI, CharacterStatPanel, CombatEventBus, CounterMechanics, Projectile +1 more |
| `float HealthPercentage` | EnemyHealthUI |
| `float IncomingDamageMultiplier` | UnrestrainedViolenceExSkill |
| `float MaxHealth` | BossHealthBarUI, CharacterStatPanel, CombatFeelRunner, DamagePopup |
| `void AssignShield(Shield newShield)` | AbsoluteContainmentExSkill, ExSkill, FairExchangeExSkill, UnknownOutcomeExSkill |
| `void Heal(float amount)` | PerfectAnnotationExSkill |
| `void RemoveShield()` | AbsoluteContainmentExSkill, ExSkill, FairExchangeExSkill, UnknownOutcomeExSkill |
| `void Revive(float healthFraction = 1)` | CharacterLifeController |
| `void SetArmor(float value)` | Character |
| `void SetDefensePower(float newDefensePower)` | EnemyDataBinder |
| `void SetInvulnerable(float seconds)` | AnnihilatorManifestation, CharacterSwitcher, SwitchRewards |
| `void SetMaxHealth(float newMaxHealth, bool refillHealth = true)` | Character, EnemyDataBinder |
| `void SpawnDeathVFX()` | EnemyLifecycle |
| `void TakeDamage(float amount)` | CounterMechanics |

### `Game/03_Gameplay/Interface/IDamageable.cs`

| Member | Used by |
|---|---|
| `void TakeDamage(float amount)` | Projectile |

### `Game/03_Gameplay/Status/Shield.cs`

| Member | Used by |
|---|---|
| `event Action OnBroken` | AbsoluteContainmentExSkill, FairExchangeExSkill, UnknownOutcomeExSkill |
| `event Action<float> OnAmountChanged` | AbsoluteContainmentExSkill, FairExchangeExSkill |
| `float Current` | AbsoluteContainmentExSkill, FairExchangeExSkill, UnknownOutcomeExSkill |
| `float Max` | AbsoluteContainmentExSkill, FairExchangeExSkill |
| `bool Absorb(ref float damage)` | AbsoluteContainmentExSkill, Health |
| `void Set(float amount)` | AbsoluteContainmentExSkill, FairExchangeExSkill, UnknownOutcomeExSkill |

### `Game/03_Gameplay/Generic/DamagePopup.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Generic/SquashBlink.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Generic/Firepoint.cs`

_Scene/prefab component: no members called by other classes._

## Hierarchy counters

Type matchup damage multipliers.

### `Game/03_Gameplay/Generic/CounterMechanics.cs`

| Member | Used by |
|---|---|
| `static float GetMultiplier(CharacterType attacker, CharacterType target)` | AnnihilatorHierarchyChoiceUI, LevelEnemyIntel, SwitchRewards |
| `static float GetMultiplier(CharacterType? attacker, CharacterType? target)` | Projectile |
| `static void Damage(Character attacker, Health target, float amount)` | ArcaneImpossibilityExSkill, SignalOverloadExSkill, SwitchRewards, TotalWarfareExSkill |

### `UI/Generic/HierarchyGuide.cs`

| Member | Used by |
|---|---|
| `static string BuildBody()` | TeamSelectionPopupUI |
| `static string Color(CharacterType type)` | AnnihilatorManifestation |
| `static string Tag(CharacterType type)` | AnnihilatorHierarchyChoiceUI, LevelEnemyIntel |

### `UI/Generic/LevelEnemyIntel.cs`

| Member | Used by |
|---|---|
| `Sprite Entry.Sprite` | PixelArt |
| `Texture2D Entry.Texture` | PixelArt |
| `int Entry.LastFrame` | PixelArt |
| `int Entry.Pixels` | PixelArt |
| `new Entry()` | PixelArt |
| `static string LevelEnemyIntel.BuildBody(LevelData level)` | TeamSelectionPopupUI |
| `static string LevelEnemyIntel.Title(LevelData level)` | TeamSelectionPopupUI |

## Status effects

Marks and debuffs on units.

### `Game/03_Gameplay/Status/StatusEffect.cs`

| Member | Used by |
|---|---|
| `event Action<StatusEffect> OnExpired` | StatusEffectSystem |
| `int CurrentStacks` | StatusEffectSystem |
| `string EffectId` | StatusEffectWorldUI |
| `void Apply(int stacks = 1)` | StatusEffectSystem |
| `void Configure(string effectId, float duration, int stacks = 1, bool refreshOnReapply = true)` | StatusEffectSystem |
| `void Refresh()` | StatusEffectSystem |
| `void Tick(float deltaTime)` | StatusEffectSystem |

### `Game/03_Gameplay/Status/StatusEffectSystem.cs`

| Member | Used by |
|---|---|
| `event Action<GameObject, StatusEffect> OnEffectAdded` | StatusEffectWorldUI |
| `event Action<GameObject, StatusEffect> OnEffectRemoved` | StatusEffectWorldUI |
| `static StatusEffectSystem Instance` | SkillTargets, StatusEffectWorldUI |
| `StatusEffect Add(GameObject target, string effectId, float duration, int stacks = 1, bool refreshOnReapply = true)` | SkillTargets |
| `void Remove(GameObject target, string effectId)` | SkillTargets |

### `Game/03_Gameplay/Status/StatusEffectWorldUI.cs`

_Scene/prefab component: no members called by other classes._

## Combat events

Team damage and kill notifications.

### `Game/03_Gameplay/Events/CombatEventBus.cs`

| Member | Used by |
|---|---|
| `static event Action<Character, Health> OnEnemyKilled` | ArcaneImpossibilityExSkill, PerfectAnnotationExSkill |
| `static event Action<Character, float> OnTeamDealtDamage` | RecursiveOptimizationExSkill |
| `static void InvokeEnemyDiedMarked(Character killer, Character enemy)` | Projectile |
| `static void ReportDamage(Character attacker, Health target, float hpBefore, bool wasAliveBefore)` | CounterMechanics, Projectile |

## Combat feel / VFX

Hitstop, slow motion, shake and pixel effects.

### `Game/03_Gameplay/Feel/CombatFeel.cs`

| Member | Used by |
|---|---|
| `static CombatFeelRunner Current` | CombatFeelShakeExtension |
| `static Coroutine Run(IEnumerator routine)` | ExSkill |
| `static void Ensure()` | CharacterSwitcher |
| `static void Flash(Color color, float duration = 0.15)` | AllOrNothingExSkill, AnnihilatorManifestation, ArcaneImpossibilityExSkill, CharacterSwitcher, CombatFeelRunner +5 more |
| `static void HeavyImpact()` | ArcaneImpossibilityExSkill |
| `static void HitStop(float duration, float timeScale = 0.02)` | AbsoluteContainmentExSkill, AllOrNothingExSkill, CharacterSwitcher, CombatFeelRunner, SignalOverloadExSkill +3 more |
| `static void LightImpact()` | ArcaneImpossibilityExSkill |
| `static void MediumImpact()` | SkillVFX |
| `static void Shake(float strength, float duration = 0.2)` | AbsoluteContainmentExSkill, AllOrNothingExSkill, AnnihilatorManifestation, CharacterSwitcher, CombatFeelRunner +9 more |
| `static void SlowMo(float duration, float timeScale = 0.2, bool letterbox = true)` | AnnihilatorManifestation, ExSkillController, StoryBattleController, SwitchRewards |
| `static void Stop(Coroutine routine)` | ExSkill |

### `Game/03_Gameplay/Feel/CombatFeelRunner.cs`

| Member | Used by |
|---|---|
| `Vector3 ShakeOffset` | CombatFeelShakeExtension |
| `static CombatFeelRunner Create()` | CombatFeel |
| `void Flash(Color color, float duration)` | CombatFeel |
| `void HitStop(float duration, float timeScale)` | CombatFeel |
| `void Shake(float strength, float duration)` | CombatFeel |
| `void SlowMo(float duration, float timeScale, bool letterboxBars)` | CombatFeel |

### `Game/03_Gameplay/Feel/CombatFeelShakeExtension.cs`

_Scene/prefab component: no members called by other classes._

### `Game/03_Gameplay/Feel/SkillVFX.cs`

| Member | Used by |
|---|---|
| `static Material Material` | VfxBeam, VfxGhost, VfxRing |
| `static TMP_FontAsset Font` | EnergyRecoveryPopupUI, InfoPopupUI, VfxText |
| `static int SortingLayerId` | PlayAreaCameraFit, VfxBeam, VfxRing, VfxText |
| `static Color WithAlpha(Color c, float a)` | AbsoluteContainmentExSkill, AllOrNothingExSkill, AnnihilatorManifestation, ArcaneImpossibilityExSkill, CharacterSwitcher +14 more |
| `static ParticleSystem Sparks(Vector3 position, Color color, int count = 14, float speed = 5, float lifetime = 0.4, float size = 0.12)` | AbsoluteContainmentExSkill, AllOrNothingExSkill, AnnihilatorManifestation, ArcaneImpossibilityExSkill, CharacterSwitcher +11 more |
| `static VfxBeam Beam(Transform from, Transform to, Color color, float width, float duration, float jitter = 0.15)` | SignalOverloadExSkill |
| `static VfxBeam Beam(Vector3 from, Vector3 to, Color color, float width, float duration, float jitter = 0.15)` | ArcaneImpossibilityExSkill, PerfectAnnotationExSkill, SignalOverloadExSkill, TotalWarfareExSkill |
| `static VfxRing Aura(Transform follow, Color color, float radius, float width, float duration = -1, float pulse = 0.06, int points = 0, int step = 1, float rotationSpeed = 0)` | AbsoluteContainmentExSkill, AllOrNothingExSkill, AnnihilatorManifestation, EmergencyRegulationExSkill, FairExchangeExSkill +6 more |
| `static VfxRing Marker(Health target, Color color, float duration, int points = 4, int step = 1, float rotationSpeed = 90)` | ArcaneImpossibilityExSkill, PerfectAnnotationExSkill, SignalOverloadExSkill, TotalWarfareExSkill |
| `static VfxRing Ring(Vector3 position, Color color, float fromRadius, float toRadius, float duration, float width, int points = 0, int step = 1, float rotationSpeed = 0)` | AbsoluteContainmentExSkill, AllOrNothingExSkill, AnnihilatorManifestation, ArcaneImpossibilityExSkill, CharacterSwitcher +11 more |
| `static VfxText Label(Transform follow, Vector3 offset, string text, Color color, float size = 3.5)` | AllOrNothingExSkill, AnnihilatorManifestation, FairExchangeExSkill, RecursiveOptimizationExSkill, TotalWarfareExSkill +2 more |
| `static VfxText Text(Vector3 position, string text, Color color, float size = 5, float duration = 0.9, float rise = 0.8)` | AbsoluteContainmentExSkill, AllOrNothingExSkill, AnnihilatorManifestation, ExSkill, ExSkillController +5 more |
| `static float GetVisualRadius(Transform root)` | AllOrNothingExSkill, CharacterSwitcher, EmergencyRegulationExSkill, ExSkillController, FairExchangeExSkill +7 more |
| `static void Activation(Character owner, string skillName, Color color)` | ExSkillController, FairExchangeExSkill |
| `static void Explosion(Vector3 position, Color color, float radius, bool withFeel = true)` | ArcaneImpossibilityExSkill, FairExchangeExSkill, SignalOverloadExSkill, TotalWarfareExSkill |
| `static void Ghost(Transform root, Color tint, float duration = 0.35, float scaleTo = 1.5)` | AnnihilatorManifestation, CharacterSwitcher, FairExchangeExSkill, SwitchRewards, UnknownOutcomeExSkill |

### `Game/03_Gameplay/Feel/VfxRing.cs`

| Member | Used by |
|---|---|
| `Color Color` | AbsoluteContainmentExSkill, FairExchangeExSkill, UnrestrainedViolenceExSkill, ViralSensationExSkill |
| `float Radius` | RecursiveOptimizationExSkill, TotalWarfareExSkill, UnrestrainedViolenceExSkill, ViralSensationExSkill |
| `float Width` | AbsoluteContainmentExSkill, FairExchangeExSkill, RecursiveOptimizationExSkill, UnrestrainedViolenceExSkill, ViralSensationExSkill |
| `static VfxRing Create(string name, Vector3 position, Transform follow)` | SkillVFX |
| `void ConfigureAura(Color c, float radius, float lineWidth, float time, float pulseAmount)` | SkillVFX |
| `void ConfigureBurst(Color c, float fromRadius, float toRadius, float time, float lineWidth)` | SkillVFX |
| `void SetShape(int pointCount, int starStep, float rotationDegreesPerSecond)` | SkillVFX |
| `void Stop(float fadeDuration = 0.2)` | AbsoluteContainmentExSkill, AllOrNothingExSkill, AnnihilatorManifestation, ArcaneImpossibilityExSkill, EmergencyRegulationExSkill +9 more |
| `void StopWhenDead(Health health)` | SkillVFX |

### `Game/03_Gameplay/Feel/VfxBeam.cs`

| Member | Used by |
|---|---|
| `Color Color` | SignalOverloadExSkill |
| `static VfxBeam Create()` | SkillVFX |
| `void Configure(Transform fromTransform, Transform toTransform, Vector3 fromPos, Vector3 toPos, Color c, float lineWidth, float time, float jitterAmount)` | SkillVFX |
| `void Stop(float fadeDuration = 0.15)` | SignalOverloadExSkill |

### `Game/03_Gameplay/Feel/VfxText.cs`

| Member | Used by |
|---|---|
| `static VfxText Create(string value, Color color, float size, Vector3 position, Transform follow, Vector3 offset)` | SkillVFX |
| `void ConfigureCallout(float time, float riseHeight)` | SkillVFX |
| `void ConfigurePersistent()` | SkillVFX |
| `void SetColor(Color c)` | AllOrNothingExSkill, ViralSensationExSkill |
| `void SetText(string value, bool doPunch = true)` | AllOrNothingExSkill, AnnihilatorManifestation, RecursiveOptimizationExSkill, TotalWarfareExSkill, UnrestrainedViolenceExSkill +1 more |
| `void Stop()` | AllOrNothingExSkill, AnnihilatorManifestation, FairExchangeExSkill, RecursiveOptimizationExSkill, TotalWarfareExSkill +2 more |

### `Game/03_Gameplay/Feel/VfxGhost.cs`

| Member | Used by |
|---|---|
| `static VfxGhost Create(SpriteRenderer source, Color tint, float duration, float scaleTo)` | SkillVFX |

### `Game/03_Gameplay/Feel/PixelArt.cs`

| Member | Used by |
|---|---|
| `static float PixelSize` | SkillVFX, VfxBeam |
| `static float PixelsPerUnit` | VfxBeam |
| `static Sprite Shape(int radius, int thickness, int points, int step, float angleDegrees)` | VfxRing |
| `static Vector3 Snap(Vector3 position)` | VfxRing |
| `static float StepAlpha(float alpha)` | VfxBeam, VfxGhost, VfxRing |
| `static int ToPixels(float world)` | VfxBeam, VfxRing |
| `static void DrawLine(Color32[] buffer, int width, int height, int x0, int y0, int x1, int y1, int thickness)` | VfxBeam |

### `Utils/VFX/SpriteVFX.cs`

_Scene/prefab component: no members called by other classes._

### `Utils/VFX/VFXCleaner.cs`

| Member | Used by |
|---|---|
| `void Clean()` | SpriteVFX |

## Combat HUD

Character stat panels and Trady's trade choice.

### `UI/HUD/CharacterStat/CharacterStatPanel.cs`

| Member | Used by |
|---|---|
| `void Initialize(Character targetCharacter, ExSkillController skillController)` | CharacterStatUI |

### `UI/HUD/CharacterStat/CharacterStatUI.cs`

_Scene/prefab component: no members called by other classes._

### `UI/HUD/TradyExchangeChoiceUI.cs`

| Member | Used by |
|---|---|
| `bool IsOpen` | FairExchangeExSkill |
| `void Close()` | FairExchangeExSkill |
| `void Show(Character ownerCharacter, float durationSeconds, Action onDefenseChosen, Action onAttackChosen)` | FairExchangeExSkill |

