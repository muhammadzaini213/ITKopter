
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterListItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button detailsButton;

    [Header("Character Classification UI")]
    [SerializeField] private Image roleImage;
    [SerializeField] private Image hierarchyImage;

    [Header("Role Sprites")]
    [SerializeField] private RoleSpriteMapping[] roleSprites;

    [Header("Hierarchy / Type Sprites")]
    [SerializeField] private TypeSpriteMapping[] typeSprites;

    private Action onDetailsClicked;

    [Serializable]
    private struct RoleSpriteMapping
    {
        public CharacterRole role;
        public Sprite sprite;
    }

    [Serializable]
    private struct TypeSpriteMapping
    {
        public CharacterType type;
        public Sprite sprite;
    }

    private void Awake()
    {
        if (detailsButton != null)
            detailsButton.onClick.AddListener(HandleDetailsClicked);
    }

    private void OnDestroy()
    {
        if (detailsButton != null)
            detailsButton.onClick.RemoveListener(HandleDetailsClicked);
    }

    public void Setup(
        CharacterData data,
        CharacterProgression progression,
        Action detailsCallback)
    {
        if (data == null || progression == null)
            return;

        onDetailsClicked = detailsCallback;

        if (iconImage != null)
        {
            iconImage.sprite = data.Icon;
            iconImage.enabled = data.Icon != null;
        }

        if (nameText != null)
            nameText.text = data.CharacterName;

        if (levelText != null)
            levelText.text = $"Lv. {progression.Level}";

        UpdateRoleImage(data.CharacterRole);
        UpdateHierarchyImage(data.CharacterType);
    }

    private void UpdateRoleImage(CharacterRole role)
    {
        Sprite sprite = null;

        foreach (var mapping in roleSprites)
        {
            if (mapping.role == role)
            {
                sprite = mapping.sprite;
                break;
            }
        }

        SetImage(roleImage, sprite);
    }

    private void UpdateHierarchyImage(CharacterType type)
    {
        Sprite sprite = null;

        foreach (var mapping in typeSprites)
        {
            if (mapping.type == type)
            {
                sprite = mapping.sprite;
                break;
            }
        }

        SetImage(hierarchyImage, sprite);
    }

    private void SetImage(Image target, Sprite sprite)
    {
        if (target == null)
            return;

        target.sprite = sprite;
        target.enabled = sprite != null;
    }

    private void HandleDetailsClicked()
    {
        onDetailsClicked?.Invoke();
    }
}
