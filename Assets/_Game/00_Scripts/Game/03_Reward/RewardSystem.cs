
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Slafurry.Core.Abstract;

public class RewardSystem : GameSystem<RewardSystem>
{
    public override IEnumerator Initialize()
    {
        yield return null;
    }

    public override void PostInitialize()
    {
    }

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();
    }

    public List<RewardGrantResult> GrantReward(
        RewardData reward,
        int amount = 1)
    {
        return GrantRewards(
            new List<RewardGrantRequest>
            {
                new RewardGrantRequest(reward, amount)
            }
        );
    }

    public List<RewardGrantResult> GrantRewards(
        IReadOnlyList<RewardGrantRequest> requests)
    {
        List<RewardGrantResult> results = new();

        if (requests == null)
            return results;

        foreach (RewardGrantRequest request in requests)
        {
            if (request == null || request.reward == null)
            {
                results.Add(CreateFailure(
                    request?.reward, request?.amount ?? 0,
                    "Reward data is missing."));
                continue;
            }

            results.Add(GrantSingleReward(
                request.reward, request.amount));
        }

        return results;
    }

    private RewardGrantResult GrantSingleReward(
        RewardData reward,
        int amount)
    {
        if (amount <= 0)
        {
            return CreateFailure(
                reward, amount, "Amount must be greater than zero.");
        }

        // Currency rewards
        if (reward is CurrencyRewardData currencyReward)
        {
            switch (currencyReward.CurrencyType)
            {
                case CurrencyType.Gems:
                    CurrencySystem.Instance.AddGems(amount);
                    break;

                case CurrencyType.Coins:
                    CurrencySystem.Instance.AddCoins(amount);
                    break;

                default:
                    return CreateFailure(
                        reward, amount, "Unsupported currency type.");
            }

            return new RewardGrantResult(
                reward, amount, amount, true, "Currency granted.");
        }

        // Character rewards
        if (reward is CharacterData character)
        {
            if (amount != 1)
            {
                return CreateFailure(
                    reward, amount,
                    "Character rewards currently support amount = 1.");
            }

            bool added =
                PlayerSystem.Instance.AddNewOwnedCharacter(character);

            if (!added)
            {
                return CreateFailure(
                    reward, amount,
                    "Character could not be added. It may already be owned.");
            }

            return new RewardGrantResult(
                reward, amount, 1, true, "Character granted.");
        }

        // Item rewards will be connected to InventorySystem.
        return CreateFailure(
            reward, amount,
            $"No reward handler registered for {reward.GetType().Name}.");
    }

    public bool CanGrantRewards(
    IReadOnlyList<RewardGrantRequest> requests,
    out string error)
    {
        error = null;

        if (requests == null || requests.Count == 0)
        {
            error = "Reward batch is empty.";
            return false;
        }

        HashSet<string> characterIds = new HashSet<string>();

        foreach (RewardGrantRequest request in requests)
        {
            if (request == null || request.reward == null)
            {
                error = "Reward request or reward data is null.";
                return false;
            }

            if (request.amount <= 0)
            {
                error = "Reward amount must be greater than zero.";
                return false;
            }

            // Currency reward validation
            if (request.reward is CurrencyRewardData currency)
            {
                if (currency.CurrencyType != CurrencyType.Coins &&
                    currency.CurrencyType != CurrencyType.Gems)
                {
                    error = "Unsupported currency type.";
                    return false;
                }

                continue;
            }

            // Character reward validation
            if (request.reward is CharacterData character)
            {
                if (request.amount != 1)
                {
                    error = "Character reward amount must be 1.";
                    return false;
                }

                if (!characterIds.Add(character.CharacterId))
                {
                    error = $"Duplicate character in reward batch: {character.name}";
                    return false;
                }

                if (!PlayerSystem.Instance.CanAddNewOwnedCharacter(character))
                {
                    error = $"Character already owned or cannot be added: {character.name}";
                    return false;
                }

                continue;
            }

            error = $"Unsupported reward type: {request.reward.GetType().Name}";
            return false;
        }

        return true;
    }

    private RewardGrantResult CreateFailure(
        RewardData reward,
        int requestedAmount,
        string message)
    {
        Debug.LogWarning($"[RewardSystem] {message}");

        return new RewardGrantResult(
            reward, requestedAmount, 0, false, message);
    }
}