
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RectTransform))]
public class UIParallax : MonoBehaviour
{
    [Header("Parallax Settings")]
    [SerializeField] private float offsetMultiplier = 50f;
    [SerializeField] private float smoothTime = 0.2f;

    private RectTransform rectTransform;

    private Vector2 startPosition;
    private Vector2 velocity;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        startPosition = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Vector2 viewportPosition = new Vector2(
            mousePosition.x / Screen.width,
            mousePosition.y / Screen.height
        );

        viewportPosition -= Vector2.one * 0.5f;

        Vector2 targetPosition =
            startPosition + viewportPosition * offsetMultiplier;

        rectTransform.anchoredPosition = Vector2.SmoothDamp(
            rectTransform.anchoredPosition,
            targetPosition,
            ref velocity,
            smoothTime
        );
    }
}
