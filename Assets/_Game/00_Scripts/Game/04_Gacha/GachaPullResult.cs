

using System;

[Serializable]
public class GachaPullResult
{
    public RewardData Reward { get; }
    public bool WasPity { get; }

    public GachaPullResult(RewardData reward, bool wasPity)
    {
        Reward = reward;
        WasPity = wasPity;
    }
}