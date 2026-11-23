using UnityEngine;

[System.Serializable]
public struct SceneTrack
{
    public SceneReference scene;
    public MusicReference music;

    public string SceneName => scene != null ? scene.SceneName : string.Empty;
    public string TrackName => music != null ? music.MusicName : string.Empty;
}

[System.Serializable]
public struct MusicTrack
{
    public MusicReference music;
    public AudioClip clip;
    [Tooltip("Intro clip played once before the main (looping) clip. Leave empty if there is no intro.")]
    public AudioClip introClip;
    [Range(0f, 1f)]
    public float volume;
    [Tooltip("Fade-in duration when this track starts playing.")]
    [SerializeField] private float fadeIn;
    [Tooltip("Fade-out duration when this track stops or changes.")]
    [SerializeField] private float fadeOut;

    public string TrackName => music != null ? music.MusicName : string.Empty;
    public float FadeIn => fadeIn > 0f ? fadeIn : 1f;
    public float FadeOut => fadeOut > 0f ? fadeOut : 1f;
}