using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Objective scene: Daily / Weekly / Monthly / Achievement tabs, list and Claim All.</summary>
public class ObjectiveMenuUI : MonoBehaviour
{
    [Serializable]
    private class Tab
    {
        public ObjectivePeriod period;
        public Button button;
        public TMP_Text label;
        public GameObject badge;
        public TMP_Text badgeText;
    }

    [SerializeField] private Tab[] tabs = new Tab[0];
    [SerializeField] private Color selectedTabColor = new Color(0.098f, 0.941f, 1f, 1f);
    [SerializeField] private Color idleTabColor = Color.white;

    [Header("List")]
    [SerializeField] private Transform listRoot;
    [SerializeField] private ObjectiveRowUI rowPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Header")]
    [SerializeField] private TMP_Text periodTitleText;
    [SerializeField] private TMP_Text resetText;
    [SerializeField] private Button claimAllButton;

    [Header("Greeting")]
    [SerializeField] private CharacterData greeter;
    [SerializeField, TextArea] private string greeting = "Consistency is a ruler's greatest weapon, Your Majesty. Your objectives are listed here.";

    private ObjectivePeriod current = ObjectivePeriod.Daily;
    private ObjectiveSystem system;
    private float timer;

    private void Start()
    {
        system = ObjectiveSystem.Instance;

        foreach (Tab tab in tabs)
        {
            ObjectivePeriod period = tab.period;

            if (tab.button != null)
                tab.button.onClick.AddListener(() => Select(period));
        }

        if (claimAllButton != null)
            claimAllButton.onClick.AddListener(ClaimAll);

        if (system != null)
        {
            system.OnObjectivesChanged += Refresh;
            system.RefreshStateObjectives();
        }

        Select(ObjectivePeriod.Daily);

        if (greeter != null && !string.IsNullOrWhiteSpace(greeting))
            StorySystem.Instance?.Toasts?.Show(new StoryToastLine { speaker = greeter, text = greeting, duration = 5f });
    }

    private void OnDestroy()
    {
        if (system != null)
            system.OnObjectivesChanged -= Refresh;
    }

    private void Update()
    {
        timer -= Time.unscaledDeltaTime;

        if (timer > 0f)
            return;

        timer = 1f;
        RefreshResetText();
    }

    public void Select(ObjectivePeriod period)
    {
        current = period;
        Refresh();

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    private void ClaimAll()
    {
        system?.ClaimAll(current);
    }

    private void Claim(ObjectiveDefinition def)
    {
        system?.Claim(def);
    }

    private void Refresh()
    {
        if (system == null || listRoot == null || rowPrefab == null)
            return;

        foreach (Transform child in listRoot)
            Destroy(child.gameObject);

        // Claimable first, then in progress, then claimed.
        IEnumerable<ObjectiveDefinition> ordered = system.GetObjectives(current)
            .Select((def, index) => (def, index))
            .OrderBy(x => system.CanClaim(x.def) ? 0 : system.IsClaimed(x.def) ? 2 : 1)
            .ThenBy(x => x.index)
            .Select(x => x.def);

        foreach (ObjectiveDefinition def in ordered)
        {
            ObjectiveRowUI row = Instantiate(rowPrefab, listRoot);
            row.Setup(def, system.GetProgress(def), system.IsClaimed(def), Claim);
        }

        foreach (Tab tab in tabs)
        {
            bool selected = tab.period == current;

            if (tab.label != null)
                tab.label.color = selected ? selectedTabColor : idleTabColor;

            int claimable = system.GetClaimableCount(tab.period);

            if (tab.badge != null)
                tab.badge.SetActive(claimable > 0);

            if (tab.badgeText != null)
                tab.badgeText.text = claimable.ToString();
        }

        if (periodTitleText != null)
            periodTitleText.text = current switch
            {
                ObjectivePeriod.Daily => "Daily Objectives",
                ObjectivePeriod.Weekly => "Weekly Objectives",
                ObjectivePeriod.Monthly => "Monthly Objectives",
                _ => "Achievements"
            };

        if (claimAllButton != null)
            claimAllButton.interactable = system.GetClaimableCount(current) > 0;

        RefreshResetText();
    }

    private void RefreshResetText()
    {
        if (resetText == null || system == null)
            return;

        if (current == ObjectivePeriod.Achievement)
        {
            resetText.text = "Never resets";
            return;
        }

        TimeSpan left = system.TimeUntilReset(current);
        resetText.text = left.TotalDays >= 1
            ? $"Resets in {(int)left.TotalDays}d {left.Hours}h"
            : $"Resets in {left.Hours:00}:{left.Minutes:00}:{left.Seconds:00}";
    }
}
