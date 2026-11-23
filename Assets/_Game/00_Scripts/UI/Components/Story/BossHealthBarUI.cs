using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-of-screen health bars for living bosses (<see cref="EnemyBoss"/>), up to the number of rows.
/// Lives on the persistent story overlay; hidden when no boss is on the field.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [Tooltip("Inactive row: children Name (TMP), Fill (Image, Filled), optional Icon (Image).")]
    [SerializeField] private GameObject rowTemplate;
    [SerializeField, Min(1)] private int maxRows = 3;

    private readonly List<(GameObject go, TMP_Text name, Image fill, Image icon)> rows = new();
    private readonly List<EnemyBoss> alive = new();

    private void Awake()
    {
        if (rowTemplate != null)
            rowTemplate.SetActive(false);

        if (root != null)
            root.SetActive(false);
    }

    private void LateUpdate()
    {
        alive.Clear();

        foreach (EnemyBoss boss in EnemyBoss.Active)
        {
            if (boss != null && boss.Health != null && !boss.Health.IsDead)
                alive.Add(boss);
        }

        bool show = alive.Count > 0;

        if (root != null && root.activeSelf != show)
            root.SetActive(show);

        if (!show || rowTemplate == null)
            return;

        int count = Mathf.Min(alive.Count, maxRows);

        while (rows.Count < count)
        {
            GameObject go = Instantiate(rowTemplate, rowTemplate.transform.parent);
            Transform icon = go.transform.Find("Icon");
            rows.Add((
                go,
                go.transform.Find("Name")?.GetComponent<TMP_Text>(),
                go.transform.Find("Bar/Fill")?.GetComponent<Image>(),
                icon != null ? icon.GetComponent<Image>() : null));
        }

        for (int i = 0; i < rows.Count; i++)
        {
            bool used = i < count;

            if (rows[i].go.activeSelf != used)
                rows[i].go.SetActive(used);

            if (!used)
                continue;

            EnemyBoss boss = alive[i];

            if (rows[i].name != null)
                rows[i].name.text = boss.DisplayName.ToUpperInvariant();

            if (rows[i].fill != null)
            {
                rows[i].fill.fillAmount = boss.Health.MaxHealth > 0f
                    ? Mathf.Clamp01(boss.Health.CurrentHealth / boss.Health.MaxHealth)
                    : 0f;
                rows[i].fill.color = boss.BarColor;
            }

            if (rows[i].icon != null)
            {
                Sprite sprite = boss.Character != null ? boss.Character.Icon : null;
                rows[i].icon.sprite = sprite;
                rows[i].icon.enabled = sprite != null;
            }
        }
    }
}
