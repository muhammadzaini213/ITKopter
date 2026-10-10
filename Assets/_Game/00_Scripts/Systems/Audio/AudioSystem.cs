using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Slafurry.Core.Abstract;

namespace Slafurry.Systems.Audio
{
    public static class Audio
    {
        public static void PlayMusic(string trackName, float fade = -1f)
            => AudioSystem.Music.PlayMusic(trackName, fade);

        public static void PlayMusic(MusicReference music, float fade = -1f)
        {
            if (music == null)
            {
                Debug.LogError("[Audio] MusicReference is null");
                return;
            }

            PlayMusic(music.MusicName, fade);
        }

        public static void StopMusic(float fade = -1f)
            => AudioSystem.Music.StopMusic(fade);

        // fadeIn/fadeOut: -1f (default) = pakai setting per-effect dari SFXEffect, isi manual untuk override
        public static void PlaySFX2D(string category, string effect, bool loop = false, float fadeIn = -1f)
            => AudioSystem.SFX.PlaySFX2D(category, effect, loop, fadeIn);

        public static void PlaySFX2D(SFXReference sfx, bool loop = false, float fadeIn = -1f)
        {
            if (sfx == null)
            {
                Debug.LogError("[Audio] SFXReference is null");
                return;
            }

            PlaySFX2D(sfx.Category, sfx.SFXName, loop, fadeIn);
        }

        public static void PlaySFX3D(string category, string effect, Vector3 pos, bool loop = false, float fadeIn = -1f)
            => AudioSystem.SFX.PlaySFX3D(category, effect, pos, loop, fadeIn);

        public static void PlaySFX3D(SFXReference sfx, Vector3 pos, bool loop = false, float fadeIn = -1f)
        {
            if (sfx == null)
            {
                Debug.LogError("[Audio] SFXReference is null");
                return;
            }

            PlaySFX3D(sfx.Category, sfx.SFXName, pos, loop, fadeIn);
        }

        public static void StopSFX(float fadeOut = -1f)
            => AudioSystem.SFX.StopAllSFX(fadeOut);

        public static void StopSFX(string category, float fadeOut = -1f)
            => AudioSystem.SFX.StopCategory(category, fadeOut);

        public static void StopSFX(string category, string effect, float fadeOut = -1f)
            => AudioSystem.SFX.StopSFX(category, effect, fadeOut);

        public static void StopSFX(SFXReference sfx, float fadeOut = -1f)
        {
            if (sfx == null)
            {
                Debug.LogError("[Audio] SFXReference is null");
                return;
            }

            StopSFX(sfx.Category, sfx.SFXName, fadeOut);
        }
    }

    public class AudioSystem : GameSystem<AudioSystem>
    {
        [Header("Mixer")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Sub Players")]
        [SerializeField] private MusicPlayer musicPlayer;
        [SerializeField] private SFXPlayer sfxPlayer;

        public static MusicPlayer Music => Instance.musicPlayer;
        public static SFXPlayer SFX => Instance.sfxPlayer;

        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;

        private const string MasterKey = "MasterVolume";
        private const string MusicKey  = "MusicVolume";
        private const string SFXKey    = "SFXVolume";

        private const string MasterPref = "MasterVolume_dB";
        private const string MusicPref  = "MusicVolume_dB";
        private const string SFXPref    = "SFXVolume_dB";

        public const float MinDb = -80f;
        public const float MaxDb = 20f;
        public const float DefaultDb = 0f;

        // ======================== GAME SYSTEM LIFECYCLE ========================

        public override IEnumerator Initialize()
        {
            LoadVolume();

            yield return new WaitForSecondsRealtime(0.1f);
        }

        public override void PostInitialize()
        {
            musicPlayer.Initialize();
            LoadVolume(); 
            PlaySceneMusic();
        }

        // ======================== VOLUME LOADER ========================

        public void LoadVolume()
        {
            ApplyDb(MasterKey, PlayerPrefs.GetFloat(MasterPref, DefaultDb));
            ApplyDb(MusicKey,  PlayerPrefs.GetFloat(MusicPref,  DefaultDb));
            ApplyDb(SFXKey,    PlayerPrefs.GetFloat(SFXPref,    DefaultDb));
        }

        // ======================== PUBLIC API ========================

        public void PlaySceneMusic()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            musicPlayer.PlayMusic(currentSceneName);
        }

        // ======================== VOLUME CHANGER (dalam dB) ========================

        public void UpdateMasterVolume(float db)
        {
            db = ApplyDb(MasterKey, db);

            PlayerPrefs.SetFloat(MasterPref, db);
            PlayerPrefs.Save();

            OnMasterVolumeChanged?.Invoke(db);
        }

        public void UpdateMusicVolume(float db)
        {
            db = ApplyDb(MusicKey, db);

            PlayerPrefs.SetFloat(MusicPref, db);
            PlayerPrefs.Save();

            OnMusicVolumeChanged?.Invoke(db);
        }

        public void UpdateSFXVolume(float db)
        {
            db = ApplyDb(SFXKey, db);

            PlayerPrefs.SetFloat(SFXPref, db);
            PlayerPrefs.Save();

            OnSFXVolumeChanged?.Invoke(db);
        }

        // ======================== UTILITIES ========================

        private float ApplyDb(string param, float db)
        {
            db = Mathf.Clamp(db, MinDb, MaxDb);
            audioMixer.SetFloat(param, db);
            return db;
        }

        public static float LinearToDecibel(float linear)
            => linear > 0.0001f
                ? Mathf.Clamp(Mathf.Log10(linear) * 20f, MinDb, MaxDb)
                : MinDb;

        public static float DecibelToLinear(float db)
            => db <= MinDb ? 0f : Mathf.Pow(10f, db / 20f);

        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
        }
    }
}