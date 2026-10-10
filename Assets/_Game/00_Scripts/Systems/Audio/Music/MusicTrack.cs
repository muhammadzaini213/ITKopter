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
    [Tooltip("Clip intro yang diputar sekali sebelum clip utama (loop). Kosongkan kalau tidak ada intro.")]
    public AudioClip introClip;
    [Range(0f, 1f)]
    public float volume;
    [Tooltip("Durasi fade in saat track ini mulai diputar.")]
    [SerializeField] private float fadeIn;
    [Tooltip("Durasi fade out saat track ini berhenti atau berganti.")]
    [SerializeField] private float fadeOut;

    public string TrackName => music != null ? music.MusicName : string.Empty;
    public float FadeIn => fadeIn > 0f ? fadeIn : 1f;
    public float FadeOut => fadeOut > 0f ? fadeOut : 1f;
}