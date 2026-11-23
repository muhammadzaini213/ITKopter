
using System;
using System.Collections.Generic;

[Serializable]
public class CharacterProgressionSaveData
{
    public string characterId;
    public int level = 1;
    public int affectionPoints;
}

[Serializable]
public class PlayerSaveData
{
    public string playerName;
    public int playerExp;

    public List<string> ownedCharacterIds = new();
    public List<string> completedLevelIds = new();
    public List<string> lastTeamCharacterIds = new();
    public string showcaseCharacterId;

    public List<CharacterProgressionSaveData> characterProgressions = new();
    public List<InventoryItemSaveData> inventoryItems = new();
}
