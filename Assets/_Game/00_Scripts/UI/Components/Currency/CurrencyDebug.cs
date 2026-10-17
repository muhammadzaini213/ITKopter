
using UnityEngine;

public class CurrencyDebug : MonoBehaviour
{
    private bool showDebug = true;

    private void OnGUI()
    {
        if (!showDebug)
        {
            if (GUI.Button(new Rect(10, 10, 120, 40), "Show Debug"))
                showDebug = true;

            return;
        }

        GUILayout.BeginArea(new Rect(10, 10, 240, 260), GUI.skin.box);

        GUILayout.Label("=== GAME DEBUG ===");

        if (CurrencySystem.Instance != null)
        {
            GUILayout.Label($"Coins: {CurrencySystem.Instance.Coins}");
            GUILayout.Label($"Gems: {CurrencySystem.Instance.Gems}");

            if (GUILayout.Button("+10 Coins"))
                CurrencySystem.Instance.AddCoins(10);

            if (GUILayout.Button("+100 Gems"))
                CurrencySystem.Instance.AddGems(100);
        }
        else
        {
            GUILayout.Label("CurrencySystem not ready");
        }

        GUILayout.Space(10);

        if (EnergySystem.Instance != null)
        {
            GUILayout.Label(
                $"Energy: {EnergySystem.Instance.Energy}/" +
                $"{EnergySystem.Instance.MaxEnergy}"
            );

            if (GUILayout.Button("+1 Energy"))
                EnergySystem.Instance.AddEnergy(1);

            if (GUILayout.Button("-1 Energy"))
                EnergySystem.Instance.SpendEnergy(1);
        }
        else
        {
            GUILayout.Label("EnergySystem not ready");
        }

        if (GUILayout.Button("Hide Debug"))
            showDebug = false;

        GUILayout.EndArea();
    }
}
