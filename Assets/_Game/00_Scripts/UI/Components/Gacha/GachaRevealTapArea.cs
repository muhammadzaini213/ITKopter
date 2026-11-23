using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Full-screen tap catcher for the gacha reveal overlay (needs a raycast-target Graphic).</summary>
public class GachaRevealTapArea : MonoBehaviour, IPointerClickHandler
{
    public event Action OnTapped;

    public void OnPointerClick(PointerEventData eventData)
    {
        OnTapped?.Invoke();
    }
}
