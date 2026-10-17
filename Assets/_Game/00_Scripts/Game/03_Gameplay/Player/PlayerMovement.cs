
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Min(0f)]
    private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool movementEnabled = true;

    public bool MovementEnabled => movementEnabled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        ReadMovementInput();
    }

    private void FixedUpdate()
    {
        if (!movementEnabled)
            return;

        rb.MovePosition(
            rb.position + moveInput * moveSpeed * Time.fixedDeltaTime
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
            x -= 1f;

        if (Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
            x += 1f;

        if (Keyboard.current.sKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed)
            y -= 1f;

        if (Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed)
            y += 1f;

        moveInput = new Vector2(x, y).normalized;
    }

    public void SetMovementEnabled(bool value)
    {
        movementEnabled = value;

        if (!value)
            moveInput = Vector2.zero;
    }
}
