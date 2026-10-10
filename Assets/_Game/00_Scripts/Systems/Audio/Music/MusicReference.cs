using UnityEngine;

[CreateAssetMenu(fileName = "Music Reference", menuName = "Audio/Music Reference")]
public class MusicReference : ScriptableObject
{
    [SerializeField] private string musicName;

    public string MusicName => musicName;
}