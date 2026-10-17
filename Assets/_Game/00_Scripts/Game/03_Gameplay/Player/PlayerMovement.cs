
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private CharacterSwitcher characterSwitcher;

    [Header("Fallback Settings")]
    [SerializeField, Min(0f)]
    private float fallbackMoveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool movementEnabled = true;

    public bool MovementEnabled => movementEnabled;
    public float CurrentMoveSpeed { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (characterSwitcher == null)
        {
            Debug.LogError(
                "[PlayerMovement] CharacterSwitcher is not assigned.",
                this
            );
        }
    }

    private void Update()
    {
        ReadMovementInput();
        UpdateMoveSpeed();
    }

    private void FixedUpdate()
    {
        if (!movementEnabled)
            return;

        Vector2 movement = moveInput * CurrentMoveSpeed;

        rb.MovePosition(
            rb.position + movement * Time.fixedDeltaTime
        );
    }

    private void ReadMovementInput()
    {
        if (!movementEnabled || Keyboard.current == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        float x = 0f;
        float y = 0f;

        if (Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
        {
            x -= 1f;
        }

        if (Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            x += 1f;
        }

        if (Keyboard.current.sKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed)
        {
            y -= 1f;
        }

        if (Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed)
        {
            y += 1f;
        }

        moveInput = new Vector2(x, y).normalized;
    }

    private void UpdateMoveSpeed()
    {
        if (characterSwitcher == null)
        {
            CurrentMoveSpeed = fallbackMoveSpeed;
            return;
        }

        CurrentMoveSpeed =
            characterSwitcher.GetEffectiveMovementSpeed();
    }

    public void SetMovementEnabled(bool value)
    {
        movementEnabled = value;

        if (!value)
        {
            moveInput = Vector2.zero;
        }
    }
}
