
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Slafurry.Systems.Audio;

public class SettingsVolumeUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Labels (optional)")]
    [SerializeField] private TextMeshProUGUI musicText;
    [SerializeField] private TextMeshProUGUI sfxText;

    private bool _loading;

    private void Awake()
    {
        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
        }
    }

    private void OnEnable()
    {
        LoadSavedValues();

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(OnMusicChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
    }

    private void OnDisable()
    {
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
    }

    private void LoadSavedValues()
    {
        if (AudioSystem.Instance == null)
            return;

        _loading = true;

        if (musicSlider != null)
        {
            float musicDb = PlayerPrefs.GetFloat("MusicVolume_dB", AudioSystem.DefaultDb);
            musicSlider.value = AudioSystem.DecibelToLinear(musicDb);
            UpdateLabel(musicText, musicDb);
        }

        if (sfxSlider != null)
        {
            float sfxDb = PlayerPrefs.GetFloat("SFXVolume_dB", AudioSystem.DefaultDb);
            sfxSlider.value = AudioSystem.DecibelToLinear(sfxDb);
            UpdateLabel(sfxText, sfxDb);
        }

        _loading = false;
    }

    private void OnMusicChanged(float value)
    {
        if (_loading || AudioSystem.Instance == null)
            return;

        float db = AudioSystem.LinearToDecibel(value);
        AudioSystem.Instance.UpdateMusicVolume(db);
        UpdateLabel(musicText, db);
    }

    private void OnSFXChanged(float value)
    {
        if (_loading || AudioSystem.Instance == null)
            return;

        float db = AudioSystem.LinearToDecibel(value);
        AudioSystem.Instance.UpdateSFXVolume(db);
        UpdateLabel(sfxText, db);
    }

    private static void UpdateLabel(TextMeshProUGUI label, float db)
    {
        if (label == null)
            return;

        label.text = db <= AudioSystem.MinDb ? "-inf" : $"{db:F1} dB";
    }
}
