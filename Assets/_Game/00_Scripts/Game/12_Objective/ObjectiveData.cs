using System;
using UnityEngine;

public enum ObjectivePeriod
{
    Daily,
    Weekly,
    Monthly,
    Achievement
}

public enum ObjectiveType
{
    // Counted from game events.
    Login,
    ClearStages,
    DefeatEnemies,
    DefeatBosses,
    GachaPulls,
    SpendEnergy,
    SpendGems,
    UpgradeCharacters,
    ShopPurchases,
    /// <summary>Claim N other objectives of the same period.</summary>
    CompleteObjectives,

    // Read from the player's state.
    OwnCharacters,
    /// <summary>Highest upgrade level among owned characters.</summary>
    CharacterLevel,
    /// <summary>Clear the stage whose LevelId is Target Id.</summary>
    ClearStage,
    /// <summary>Complete the story chapter whose StoryId is Target Id.</summary>
    CompleteStory
}

[Serializable]
public class ObjectiveDefinition
{
    [Tooltip("Unique and stable: progress is saved under this id.")]
    public string id;
    public ObjectivePeriod period;
    public ObjectiveType type;
    [TextArea(1, 2)] public string description;
    [Min(1)] public int target = 1;
    [Tooltip("ClearStage: LevelId. CompleteStory: StoryId.")]
    public string targetId;
    public StoryRewardEntry[] rewards = new StoryRewardEntry[0];

    public bool IsStateBased =>
        type == ObjectiveType.OwnCharacters || type == ObjectiveType.CharacterLevel ||
        type == ObjectiveType.ClearStage || type == ObjectiveType.CompleteStory;
}

[CreateAssetMenu(fileName = "ObjectiveDatabase", menuName = "Scriptable Objects/Objective Database")]
public class ObjectiveDatabase : ScriptableObject
{
    [SerializeField] private ObjectiveDefinition[] objectives = new ObjectiveDefinition[0];

    public ObjectiveDefinition[] Objectives => objectives;
}
