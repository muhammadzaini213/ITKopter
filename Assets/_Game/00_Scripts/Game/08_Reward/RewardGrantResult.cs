public class RewardGrantResult
{
    public RewardData Reward { get; }
    public int RequestedAmount { get; }
    public int GrantedAmount { get; }
    public bool Success { get; }
    public string Message { get; }

    public RewardGrantResult(
        RewardData reward,
        int requestedAmount,
        int grantedAmount,
        bool success,
        string message)
    {
        Reward = reward;
        RequestedAmount = requestedAmount;
        GrantedAmount = grantedAmount;
        Success = success;
        Message = message;
    }
}