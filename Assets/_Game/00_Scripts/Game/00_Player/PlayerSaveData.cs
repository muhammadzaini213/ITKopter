
using System;
using System.Collections.Generic;

[Serializable]
public class PlayerSaveData
{
    public string playerName;
    public int playerExp;

    public List<string> ownedCharacterIds = new List<string>();
    public List<string> completedLevelIds = new List<string>();
}