using UnityEngine;
[CreateAssetMenu(fileName = "SFX Reference", menuName = "Audio/SFX Reference")]
public class SFXReference : ScriptableObject
{
    [SerializeField] private string category;
    [SerializeField] private string sfxName;

    public string Category => category;
    public string SFXName => sfxName;
}