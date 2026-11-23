using UnityEngine;


[System.Serializable]
public class SFXEffect
{
    public string groupID;
    public AudioClip[] clips;
    [Range(0f, 1f)]
    public float volume = 1f;
    public int maxSimultaneous = 3;

    [Header("Fade")]
    [Tooltip("Default fade-in duration (seconds) when this effect plays. 0 = full volume immediately.")]
    [Range(0f, 5f)]
    public float fadeIn = 0f;
    [Tooltip("Default fade-out duration (seconds) when this effect stops. 0 = stop immediately.")]
    [Range(0f, 5f)]
    public float fadeOut = 0f;

    [Header("Distance (used by PlaySFX3D)")]
    [Tooltip("When ticked, distance only changes the VOLUME, with no left/right panning. Good for 2D/top-down games.")]
    public bool distanceOnlyNoPanning = true;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;
    [Tooltip("Below this distance the volume is a full 100%.")]
    public float minDistance = 1f;
    [Tooltip("Beyond this distance the sound is completely inaudible.")]
    public float maxDistance = 15f;
}

namespace Slafurry.Systems.Audio
{

    [CreateAssetMenu(fileName = "SFX Category", menuName = "Audio/SFX Category")]
    public class SFXCategory : ScriptableObject
    {
        public string categoryName;
        public SFXEffect[] effects;
        public int poolSize = 8;
    }
}