using System;

[Serializable]
public class RewardGrantRequest
{
    public RewardData reward;
    public int amount;

    public RewardGrantRequest(RewardData reward, int amount = 1)
    {
        this.reward = reward;
        this.amount = amount;
    }
}