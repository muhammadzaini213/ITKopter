
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Slafurry.Core.Abstract;

public class RewardSystem : GameSystem<RewardSystem>
{
    [Header("Reward Popup")]
    [SerializeField] private RewardPopupUI rewardPopupUI;

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

    public void ShowRewardPopup(IReadOnlyList<RewardData> rewards)
    {
        if (rewardPopupUI == null)
        {
            Debug.LogWarning(
                "[RewardSystem] RewardPopupUI reference is missing.");
            return;
        }

        rewardPopupUI.ShowRewards(rewards);
    }

    public List<RewardGrantResult> GrantReward(
        RewardData reward,
        int amount = 1)
    {
        return GrantRewards(
            new List<RewardGrantRequest>
            {
                new RewardGrantRequest(reward, amount)
            });
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
                    request?.reward,
                    request?.amount ?? 0,
                    "Reward data is missing."));
                continue;
            }

            results.Add(
                GrantSingleReward(request.reward, request.amount));
        }

        return results;
    }

    private RewardGrantResult GrantSingleReward(
        RewardData reward,
        int amount)
    {
        if (reward == null)
            return CreateFailure(null, amount, "Reward data is missing.");

        if (amount <= 0)
        {
            return CreateFailure(
                reward,
                amount,
                "Amount must be greater than zero.");
        }

        // Currency rewards
        if (reward is CurrencyRewardData currencyReward)
        {
            if (CurrencySystem.Instance == null)
            {
                return CreateFailure(
                    reward,
                    amount,
                    "CurrencySystem is unavailable.");
            }

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
                        reward,
                        amount,
                        "Unsupported currency type.");
            }

            return new RewardGrantResult(
                reward,
                amount,
                amount,
                true,
                "Currency granted.");
        }

        // Character rewards
        if (reward is CharacterData character)
        {
            if (amount != 1)
            {
                return CreateFailure(
                    reward,
                    amount,
                    "Character rewards currently support amount = 1.");
            }

            if (PlayerSystem.Instance == null)
            {
                return CreateFailure(
                    reward,
                    amount,
                    "PlayerSystem is unavailable.");
            }

            // Convert duplicate characters into their configured item.
            if (!PlayerSystem.Instance.CanAddNewOwnedCharacter(character))
            {
                return ConvertDuplicateCharacter(character, amount);
            }

            bool added =
                PlayerSystem.Instance.AddNewOwnedCharacter(character);

            if (!added)
            {
                // Re-check in case the character became owned before insertion.
                if (!PlayerSystem.Instance.CanAddNewOwnedCharacter(character))
                    return ConvertDuplicateCharacter(character, amount);

                return CreateFailure(
                    reward,
                    amount,
                    "Character could not be added.");
            }

            return new RewardGrantResult(
                reward,
                amount,
                1,
                true,
                "Character granted.");
        }

        // Item rewards
        if (reward is ItemData item)
        {
            if (InventorySystem.Instance == null)
            {
                return CreateFailure(
                    reward,
                    amount,
                    "InventorySystem is unavailable.");
            }

            if (!InventorySystem.Instance.AddItem(item, amount))
            {
                return CreateFailure(
                    reward,
                    amount,
                    "Item could not be added to the inventory.");
            }

            return new RewardGrantResult(
                reward,
                amount,
                amount,
                true,
                "Item granted.");
        }

        return CreateFailure(
            reward,
            amount,
            $"No reward handler registered for {reward.GetType().Name}.");
    }

    private RewardGrantResult ConvertDuplicateCharacter(
        CharacterData character,
        int requestedAmount)
    {
        ItemData duplicateItem = character.DuplicateItem;
        int duplicateAmount = character.DuplicateItemAmount;

        if (duplicateItem == null || duplicateAmount <= 0)
        {
            return CreateFailure(
                character,
                requestedAmount,
                $"Duplicate conversion is not configured for {character.CharacterName}.");
        }

        if (InventorySystem.Instance == null)
        {
            return CreateFailure(
                character,
                requestedAmount,
                "InventorySystem is unavailable.");
        }

        if (!InventorySystem.Instance.AddItem(
                duplicateItem,
                duplicateAmount))
        {
            return CreateFailure(
                character,
                requestedAmount,
                "Duplicate conversion item could not be added.");
        }

        return new RewardGrantResult(
            character,
            requestedAmount,
            1,
            true,
            $"Duplicate character converted into {duplicateItem.name} x{duplicateAmount}.");
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

        if (PlayerSystem.Instance == null &&
            ContainsCharacterReward(requests))
        {
            error = "PlayerSystem is unavailable.";
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

                if (CurrencySystem.Instance == null)
                {
                    error = "CurrencySystem is unavailable.";
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

                if (string.IsNullOrWhiteSpace(character.CharacterId))
                {
                    error = $"Character ID is missing: {character.name}";
                    return false;
                }

                if (PlayerSystem.Instance == null)
                {
                    error = "PlayerSystem is unavailable.";
                    return false;
                }

                // An already-owned character is valid only if its conversion
                // item can be granted.
                if (!PlayerSystem.Instance.CanAddNewOwnedCharacter(character))
                {
                    if (character.DuplicateItem == null ||
                        character.DuplicateItemAmount <= 0)
                    {
                        error =
                            $"Duplicate conversion is not configured: {character.CharacterName}";
                        return false;
                    }

                    if (InventorySystem.Instance == null)
                    {
                        error = "InventorySystem is unavailable.";
                        return false;
                    }

                    if (!InventorySystem.Instance.CanAddItem(
                            character.DuplicateItem,
                            character.DuplicateItemAmount))
                    {
                        error =
                            $"Duplicate conversion item cannot be added: {character.DuplicateItem.name}";
                        return false;
                    }

                    continue;
                }

                // Prevent the same unowned character appearing twice in one batch.
                if (!characterIds.Add(character.CharacterId))
                {
                    error =
                        $"Duplicate character in reward batch: {character.CharacterName}";
                    return false;
                }

                continue;
            }

            // Item reward validation
            if (request.reward is ItemData item)
            {
                if (InventorySystem.Instance == null)
                {
                    error = "InventorySystem is unavailable.";
                    return false;
                }

                if (!InventorySystem.Instance.CanAddItem(
                        item,
                        request.amount))
                {
                    error =
                        $"Item cannot be added to inventory: {item.name}";
                    return false;
                }

                continue;
            }

            error =
                $"Unsupported reward type: {request.reward.GetType().Name}";
            return false;
        }

        return true;
    }

    private bool ContainsCharacterReward(
        IReadOnlyList<RewardGrantRequest> requests)
    {
        foreach (RewardGrantRequest request in requests)
        {
            if (request?.reward is CharacterData)
                return true;
        }

        return false;
    }

    private RewardGrantResult CreateFailure(
        RewardData reward,
        int requestedAmount,
        string message)
    {
        Debug.LogWarning($"[RewardSystem] {message}");

        return new RewardGrantResult(
            reward,
            requestedAmount,
            0,
            false,
            message);
    }
}
