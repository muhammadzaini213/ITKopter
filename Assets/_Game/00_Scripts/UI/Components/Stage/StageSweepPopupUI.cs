using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sweep a cleared stage: pick how many times (1..10, limited by energy),
/// see the total energy and rewards, confirm. Built at runtime with the
/// project's 9-slice frame; opened from the team selection popup.
/// </summary>
public class StageSweepPopupUI : MonoBehaviour
{
    private LevelData level;
    private int count = 1;
    private Action onSwept;

    private TMP_FontAsset font;
    private TMP_Text countText;
    private TMP_Text costText;
    private TMP_Text rewardText;
    private TMP_Text messageText;
    private Button minusButton;
    private Button plusButton;
    private Button maxButton;
    private Button sweepButton;

    public static StageSweepPopupUI Show(LevelData level, Action onSwept = null)
    {
        if (level == null)
            return null;

        GameObject root = RuntimeUIKit.NewOverlayCanvas("StageSweepPopup", 300);
        var ui = root.AddComponent<StageSweepPopupUI>();
        ui.level = level;
        ui.onSwept = onSwept;
        ui.Build();
        ui.Refresh();
        return ui;
    }

    private void Build()
    {
        font = RuntimeUIKit.FindFont();
        Sprite frame = RuntimeUIKit.FindFrameSprite();

        var dim = RuntimeUIKit.NewImage("Dim", transform, RuntimeUIKit.Dim, null);
        RuntimeUIKit.Stretch(dim.rectTransform);
        dim.gameObject.AddComponent<Button>().onClick.AddListener(Close);   // tap outside = cancel

        RectTransform panel = RuntimeUIKit.NewImage("Panel", transform, Color.white, frame).rectTransform;
        panel.sizeDelta = new Vector2(700f, 440f);

        string title = level.LevelName.Replace(" - BOSS", "  <color=#FF5A6E>BOSS</color>");
        RuntimeUIKit.NewText("Title", panel, font, $"SWEEP  {title}", 32f, RuntimeUIKit.Cyan, new Vector2(0f, 172f), new Vector2(640f, 46f));
        RuntimeUIKit.NewText("Subtitle", panel, font, "Instantly clear this stage again for its rewards.", 18f,
            RuntimeUIKit.SubtleText, new Vector2(0f, 134f), new Vector2(640f, 26f));

        // Count stepper: [-]  x3  [+]  [MAX]
        minusButton = RuntimeUIKit.NewButton("Minus", panel, font, frame, "-", 30f, new Vector2(-170f, 70f), new Vector2(76f, 64f), 2f);
        countText = RuntimeUIKit.NewText("Count", panel, font, "x1", 40f, Color.white, new Vector2(-50f, 70f), new Vector2(150f, 64f));
        plusButton = RuntimeUIKit.NewButton("Plus", panel, font, frame, "+", 30f, new Vector2(70f, 70f), new Vector2(76f, 64f), 2f);
        maxButton = RuntimeUIKit.NewButton("Max", panel, font, frame, "MAX", 22f, new Vector2(180f, 70f), new Vector2(110f, 64f), 2f);

        minusButton.onClick.AddListener(() => SetCount(count - 1));
        plusButton.onClick.AddListener(() => SetCount(count + 1));
        maxButton.onClick.AddListener(() => SetCount(StageSweep.MaxAffordable(level)));

        costText = RuntimeUIKit.NewText("Cost", panel, font, "", 22f, Color.white, new Vector2(0f, 4f), new Vector2(640f, 32f));
        rewardText = RuntimeUIKit.NewText("Rewards", panel, font, "", 18f, RuntimeUIKit.SubtleText, new Vector2(0f, -48f), new Vector2(620f, 64f));
        messageText = RuntimeUIKit.NewText("Message", panel, font, "", 18f, new Color(1f, 0.45f, 0.45f), new Vector2(0f, -98f), new Vector2(620f, 26f));

        sweepButton = RuntimeUIKit.NewButton("Sweep", panel, font, frame, "SWEEP", 24f, new Vector2(-110f, -158f), new Vector2(190f, 60f), 2f);
        Button cancel = RuntimeUIKit.NewButton("Cancel", panel, font, frame, "CANCEL", 22f, new Vector2(110f, -158f), new Vector2(190f, 60f), 2f);

        sweepButton.onClick.AddListener(Confirm);
        cancel.onClick.AddListener(Close);
    }

    private void SetCount(int value)
    {
        count = Mathf.Clamp(value, 1, StageSweep.MaxPerSweep);
        Refresh();
    }

    private void Refresh()
    {
        int max = StageSweep.MaxAffordable(level);
        int cost = Mathf.Max(0, level.EnergyCost);
        int energy = EnergySystem.Instance != null ? EnergySystem.Instance.Energy : 0;
        bool affordable = count <= max;

        countText.text = $"x{count}";
        costText.text = $"Energy  <color={(affordable ? "#19F0FF" : "#FF5A6E")}>{cost * count}</color>" +
                        $"  <color=#8FA9B5>({cost} each, you have {energy})</color>";
        rewardText.text = BuildRewardLine();
        messageText.text = max <= 0 ? "Not enough energy." : affordable ? "" : $"You can afford up to x{max}.";

        minusButton.interactable = count > 1;
        plusButton.interactable = count < StageSweep.MaxPerSweep;
        maxButton.interactable = max > 0;
        sweepButton.interactable = affordable && max > 0;
    }

    private string BuildRewardLine()
    {
        var sb = new StringBuilder();

        if (level.RewardEntries != null)
        {
            foreach (StoryRewardEntry entry in level.RewardEntries)
            {
                if (entry == null || entry.reward == null)
                    continue;

                if (sb.Length > 0) sb.Append("   ");
                sb.Append($"{RewardName(entry.reward)} <color=#FFD447>x{Mathf.Max(1, entry.amount) * count}</color>");
            }
        }

        if (level.ExpReward > 0)
            sb.Append($"   EXP <color=#6CFF8A>+{Mathf.RoundToInt(level.ExpReward) * count}</color>");

        return sb.Length > 0 ? sb.ToString() : "No rewards.";
    }

    private static string RewardName(RewardData reward)
    {
        string name = reward.name.Replace("Reward", string.Empty).Replace("_", " ").Trim();
        return string.IsNullOrEmpty(name) ? reward.name : name;
    }

    private void Confirm()
    {
        if (!StageSweep.Sweep(level, count, out string error))
        {
            messageText.text = error;
            Refresh();
            return;
        }

        Action callback = onSwept;
        Close();
        callback?.Invoke();
    }

    public void Close()
    {
        Destroy(gameObject);
    }
}
