
using UnityEngine;

public class CharacterBoundaryController : MonoBehaviour
{
    [Header("Boundary Settings")]
    [SerializeField] private BoxCollider2D parentCollider;

    [SerializeField, Min(0f)]
    private float boundaryPadding = 0.02f;

    public bool IsConfigured => parentCollider != null;

    public bool Initialize(BoxCollider2D arenaCollider)
    {
        if (arenaCollider == null)
        {
            Debug.LogError(
                "[CharacterBoundaryController] Arena collider is missing.",
                this);

            return false;
        }

        parentCollider = arenaCollider;
        return true;
    }

    public Vector3 ClampChildLocalPosition(
        BoxCollider2D childCollider,
        Transform childTransform,
        Vector3 desiredLocalPosition)
    {
        if (parentCollider == null ||
            childCollider == null ||
            childTransform == null)
        {
            return desiredLocalPosition;
        }

        Vector2 parentSize = parentCollider.size;
        Vector2 parentOffset = parentCollider.offset;

        Vector2 childScale = new Vector2(
            Mathf.Abs(childTransform.localScale.x),
            Mathf.Abs(childTransform.localScale.y));

        Vector2 halfChild = new Vector2(
            childCollider.size.x * childScale.x * 0.5f,
            childCollider.size.y * childScale.y * 0.5f);

        Vector2 childOffset = new Vector2(
            childCollider.offset.x * childScale.x,
            childCollider.offset.y * childScale.y);

        float minX = parentOffset.x -
                     parentSize.x * 0.5f +
                     halfChild.x + boundaryPadding;

        float maxX = parentOffset.x +
                     parentSize.x * 0.5f -
                     halfChild.x - boundaryPadding;

        float minY = parentOffset.y -
                     parentSize.y * 0.5f +
                     halfChild.y + boundaryPadding;

        float maxY = parentOffset.y +
                     parentSize.y * 0.5f -
                     halfChild.y - boundaryPadding;

        if (minX > maxX)
        {
            desiredLocalPosition.x = parentOffset.x;
        }
        else
        {
            desiredLocalPosition.x = Mathf.Clamp(
                desiredLocalPosition.x,
                minX - childOffset.x,
                maxX - childOffset.x);
        }

        if (minY > maxY)
        {
            desiredLocalPosition.y = parentOffset.y;
        }
        else
        {
            desiredLocalPosition.y = Mathf.Clamp(
                desiredLocalPosition.y,
                minY - childOffset.y,
                maxY - childOffset.y);
        }

        return desiredLocalPosition;
    }
}
