using TMPro;
using UnityEngine;

/// <summary>Notification badge (e.g. on the main menu Objective button): count of claimable objectives.</summary>
public class ObjectiveBadgeUI : MonoBehaviour
{
    [SerializeField] private GameObject badge;
    [SerializeField] private TMP_Text countText;

    private ObjectiveSystem system;

    private void Start()
    {
        system = ObjectiveSystem.Instance;

        if (system != null)
        {
            system.OnObjectivesChanged += Refresh;
            system.RefreshStateObjectives();
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (system != null)
            system.OnObjectivesChanged -= Refresh;
    }

    private void Refresh()
    {
        int count = system != null ? system.TotalClaimableCount : 0;

        if (badge != null)
            badge.SetActive(count > 0);

        if (countText != null)
            countText.text = count > 9 ? "9+" : count.ToString();
    }
}
