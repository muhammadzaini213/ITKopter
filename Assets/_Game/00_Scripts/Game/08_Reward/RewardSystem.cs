
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Slafurry.Core.Abstract;

public class RewardSystem : GameSystem<RewardSystem>
{
    [Header("Reward Popup")]
    [SerializeField] private RewardPopupUI rewardPopupUI;

    #region Initialization

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

    #endregion

    #region Reward Popup

    public void ShowRewardPopup(IReadOnlyList<RewardData> rewards)
    {
        if (rewardPopupUI == null)
        {
            Debug.LogWarning(
                "[RewardSystem] RewardPopupUI reference is missing."
            );

            return;
        }

        rewardPopupUI.ShowRewards(rewards);
    }

    #endregion

    #region Grant API

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
        List<RewardGrantResult> results =
            new List<RewardGrantResult>();

        if (requests == null)
            return results;

        foreach (RewardGrantRequest request in requests)
        {
            if (request == null || request.reward == null)
            {
                results.Add(
                    CreateFailure(
                        request?.reward,
                        request?.amount ?? 0,
                        "Reward data is missing."
                    )
                );

                continue;
            }

            results.Add(
                GrantSingleReward(
                    request.reward,
                    request.amount
                )
            );
        }

        return results;
    }

    #endregion

    #region Grant Single Reward

    private RewardGrantResult GrantSingleReward(
        RewardData reward,
        int amount)
    {
        if (reward == null)
        {
            return CreateFailure(
                null,
                amount,
                "Reward data is missing."
            );
        }

        if (amount <= 0)
        {
            return CreateFailure(
                reward,
                amount,
                "Amount must be greater than zero."
            );
        }

        // Currency Rewards
        if (reward is CurrencyRewardData currencyReward)
        {
            return GrantCurrencyReward(
                currencyReward,
                amount
            );
        }

        // Character Rewards
        if (reward is CharacterData character)
        {
            return GrantCharacterReward(
                character,
                amount
            );
        }

        // Item Rewards
        if (reward is ItemData item)
        {
            return GrantItemReward(
                item,
                amount
            );
        }

        return CreateFailure(
            reward,
            amount,
            $"No reward handler registered for {reward.GetType().Name}."
        );
    }

    #endregion

    #region Currency Rewards

    private RewardGrantResult GrantCurrencyReward(
        CurrencyRewardData currencyReward,
        int amount)
    {
        if (CurrencySystem.Instance == null)
        {
            return CreateFailure(
                currencyReward,
                amount,
                "CurrencySystem is unavailable."
            );
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
                    currencyReward,
                    amount,
                    "Unsupported currency type."
                );
        }

        return new RewardGrantResult(
            currencyReward,
            amount,
            amount,
            true,
            "Currency granted."
        );
    }

    #endregion

    #region Character Rewards

    private RewardGrantResult GrantCharacterReward(
        CharacterData character,
        int amount)
    {
        if (amount != 1)
        {
            return CreateFailure(
                character,
                amount,
                "Character rewards currently support amount = 1."
            );
        }

        if (PlayerSystem.Instance == null)
        {
            return CreateFailure(
                character,
                amount,
                "PlayerSystem is unavailable."
            );
        }

        // Check whether the player already owns this character.
        if (!PlayerSystem.Instance.CanAddNewOwnedCharacter(character))
        {
            return ConvertDuplicateCharacter(
                character,
                amount
            );
        }

        // Grant new character.
        bool added =
            PlayerSystem.Instance.AddNewOwnedCharacter(character);

        if (!added)
        {
            // Re-check in case the character became owned
            // before insertion.
            if (!PlayerSystem.Instance.CanAddNewOwnedCharacter(character))
            {
                return ConvertDuplicateCharacter(
                    character,
                    amount
                );
            }

            return CreateFailure(
                character,
                amount,
                "Character could not be added."
            );
        }

        return new RewardGrantResult(
            character,
            amount,
            1,
            true,
            "Character granted."
        );
    }

    #endregion

    #region Duplicate Character Conversion

    private RewardGrantResult ConvertDuplicateCharacter(
        CharacterData character,
        int requestedAmount)
    {
        if (character == null)
        {
            return CreateFailure(
                null,
                requestedAmount,
                "Character data is missing."
            );
        }

        ItemData duplicateDust = character.DuplicateDust;
        ItemData duplicateEngram = character.DuplicateEngram;

        int dustAmount = character.DuplicateDustAmount;
        int engramAmount = character.DuplicateEngramAmount;

        // At least one duplicate material must be configured.
        if (duplicateDust == null && duplicateEngram == null)
        {
            return CreateFailure(
                character,
                requestedAmount,
                $"Duplicate conversion is not configured for {character.CharacterName}."
            );
        }

        if (duplicateDust != null && dustAmount <= 0)
        {
            return CreateFailure(
                character,
                requestedAmount,
                "Duplicate dust amount must be greater than zero."
            );
        }

        if (duplicateEngram != null && engramAmount <= 0)
        {
            return CreateFailure(
                character,
                requestedAmount,
                "Duplicate engram amount must be greater than zero."
            );
        }

        if (InventorySystem.Instance == null)
        {
            return CreateFailure(
                character,
                requestedAmount,
                "InventorySystem is unavailable."
            );
        }

        // Combine duplicate materials.
        // This also handles the case where both fields
        // reference the same ItemData.
        Dictionary<ItemData, int> materials =
            new Dictionary<ItemData, int>();

        if (duplicateDust != null)
        {
            if (!TryAddItemAmount(
                materials,
                duplicateDust,
                dustAmount))
            {
                return CreateFailure(
                    character,
                    requestedAmount,
                    "Invalid duplicate dust amount."
                );
            }
        }

        if (duplicateEngram != null)
        {
            if (!TryAddItemAmount(
                materials,
                duplicateEngram,
                engramAmount))
            {
                return CreateFailure(
                    character,
                    requestedAmount,
                    "Invalid duplicate engram amount."
                );
            }
        }

        // Validate all materials before granting.
        foreach (KeyValuePair<ItemData, int> material in materials)
        {
            if (!InventorySystem.Instance.CanAddItem(
                material.Key,
                material.Value))
            {
                return CreateFailure(
                    character,
                    requestedAmount,
                    $"Duplicate material cannot be added: {material.Key.name}"
                );
            }
        }

        // Grant duplicate materials.
        foreach (KeyValuePair<ItemData, int> material in materials)
        {
            if (!InventorySystem.Instance.AddItem(
                material.Key,
                material.Value))
            {
                Debug.LogError(
                    "[RewardSystem] Duplicate conversion partially failed. " +
                    "Inventory rollback is not implemented."
                );

                return CreateFailure(
                    character,
                    requestedAmount,
                    $"Failed to grant duplicate material: {material.Key.name}"
                );
            }
        }

        string message =
            $"Duplicate character converted: {character.CharacterName}";

        if (duplicateDust != null)
        {
            message +=
                $" | {duplicateDust.name} x{dustAmount}";
        }

        if (duplicateEngram != null)
        {
            message +=
                $" | {duplicateEngram.name} x{engramAmount}";
        }

        Debug.Log(
            $"[RewardSystem] {message}"
        );

        return new RewardGrantResult(
            character,
            requestedAmount,
            1,
            true,
            message
        );
    }

    #endregion

    #region Item Rewards

    private RewardGrantResult GrantItemReward(
        ItemData item,
        int amount)
    {
        if (InventorySystem.Instance == null)
        {
            return CreateFailure(
                item,
                amount,
                "InventorySystem is unavailable."
            );
        }

        if (!InventorySystem.Instance.AddItem(item, amount))
        {
            return CreateFailure(
                item,
                amount,
                "Item could not be added to the inventory."
            );
        }

        return new RewardGrantResult(
            item,
            amount,
            amount,
            true,
            "Item granted."
        );
    }

    #endregion

    #region Batch Validation

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

        if (ContainsCharacterReward(requests) &&
            PlayerSystem.Instance == null)
        {
            error = "PlayerSystem is unavailable.";
            return false;
        }

        // Tracks characters that will be newly granted
        // during this batch.
        HashSet<string> scheduledCharacterIds =
            new HashSet<string>();

        // Tracks the total amount of every item that
        // needs to be added during this batch.
        Dictionary<ItemData, int> requiredItems =
            new Dictionary<ItemData, int>();

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

            // Currency validation.
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

            // Character validation.
            if (request.reward is CharacterData character)
            {
                if (request.amount != 1)
                {
                    error = "Character reward amount must be 1.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(character.CharacterId))
                {
                    error =
                        $"Character ID is missing: {character.name}";

                    return false;
                }

                // Determine whether this character will be new
                // or converted into duplicate materials.
                bool canAddAsNew =
                    PlayerSystem.Instance.CanAddNewOwnedCharacter(character);

                bool firstOccurrence =
                    scheduledCharacterIds.Add(character.CharacterId);

                bool willBeNewCharacter =
                    canAddAsNew && firstOccurrence;

                if (willBeNewCharacter)
                {
                    continue;
                }

                // Duplicate character validation.
                if (!ValidateDuplicateCharacter(
                    character,
                    requiredItems,
                    out error))
                {
                    return false;
                }

                continue;
            }

            // Item validation.
            if (request.reward is ItemData item)
            {
                if (!TryAddItemAmount(
                    requiredItems,
                    item,
                    request.amount))
                {
                    error =
                        $"Invalid or overflowing item amount: {item.name}";

                    return false;
                }

                continue;
            }

            error =
                $"Unsupported reward type: {request.reward.GetType().Name}";

            return false;
        }

        // Validate total inventory capacity.
        if (requiredItems.Count > 0)
        {
            if (InventorySystem.Instance == null)
            {
                error = "InventorySystem is unavailable.";
                return false;
            }

            foreach (KeyValuePair<ItemData, int> item in requiredItems)
            {
                if (!InventorySystem.Instance.CanAddItem(
                    item.Key,
                    item.Value))
                {
                    error =
                        $"Item cannot be added to inventory: {item.Key.name} x{item.Value}";

                    return false;
                }
            }
        }

        return true;
    }

    #endregion

    #region Duplicate Validation

    private bool ValidateDuplicateCharacter(
        CharacterData character,
        Dictionary<ItemData, int> requiredItems,
        out string error)
    {
        error = null;

        ItemData duplicateDust = character.DuplicateDust;
        ItemData duplicateEngram = character.DuplicateEngram;

        int dustAmount = character.DuplicateDustAmount;
        int engramAmount = character.DuplicateEngramAmount;

        if (duplicateDust == null && duplicateEngram == null)
        {
            error =
                $"Duplicate conversion is not configured: {character.CharacterName}";

            return false;
        }

        if (duplicateDust != null)
        {
            if (dustAmount <= 0)
            {
                error =
                    $"Invalid duplicate dust amount: {character.CharacterName}";

                return false;
            }

            if (!TryAddItemAmount(
                requiredItems,
                duplicateDust,
                dustAmount))
            {
                error =
                    $"Invalid duplicate dust amount: {character.CharacterName}";

                return false;
            }
        }

        if (duplicateEngram != null)
        {
            if (engramAmount <= 0)
            {
                error =
                    $"Invalid duplicate engram amount: {character.CharacterName}";

                return false;
            }

            if (!TryAddItemAmount(
                requiredItems,
                duplicateEngram,
                engramAmount))
            {
                error =
                    $"Invalid duplicate engram amount: {character.CharacterName}";

                return false;
            }
        }

        return true;
    }

    #endregion

    #region Item Amount Helpers

    private bool TryAddItemAmount(
        Dictionary<ItemData, int> items,
        ItemData item,
        int amount)
    {
        if (items == null || item == null || amount <= 0)
            return false;

        if (items.TryGetValue(item, out int currentAmount))
        {
            long total = (long)currentAmount + amount;

            if (total > int.MaxValue)
                return false;

            items[item] = (int)total;
        }
        else
        {
            items.Add(item, amount);
        }

        return true;
    }

    #endregion

    #region Utilities

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
        Debug.LogWarning(
            $"[RewardSystem] {message}"
        );

        return new RewardGrantResult(
            reward,
            requestedAmount,
            0,
            false,
            message
        );
    }

    #endregion
}
