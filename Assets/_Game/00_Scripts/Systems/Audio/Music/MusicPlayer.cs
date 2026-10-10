using UnityEngine;
using System.Collections;
using Slafurry.Systems.Scene;

namespace Slafurry.Systems.Audio
{
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private MusicData musicData;
        [SerializeField] private AudioSource musicSource;

        [Header("Scene Music")]
        [SerializeField] private SceneTrack[] sceneTracks;

        private Coroutine _currentFadeCoroutine;
        private Coroutine _introCoroutine;
        private MusicTrack _currentTrack;
        private bool _hasTrack;

        public void Initialize()
        {
            SceneLoader.Instance.OnSceneLoadCompleted += HandleSceneLoaded;
        }

        void OnDisable()
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.OnSceneLoadCompleted -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(string sceneName)
        {
            string trackToPlay = null;

            if (sceneTracks != null)
            {
                foreach (var st in sceneTracks)
                {
                    string registeredScene = st.SceneName;

                    if (!string.IsNullOrEmpty(registeredScene) && registeredScene == sceneName)
                    {
                        trackToPlay = st.TrackName;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(trackToPlay))
                trackToPlay = sceneName;

            if (musicData != null && musicData.GetClipFromName(trackToPlay) != null)
                PlayMusic(trackToPlay);
        }

        public void PlayMusic(MusicReference music, float fadeDuration = 0f)
        {
            if (music == null)
            {
                Debug.LogError("[MusicPlayer] MusicReference is null");
                return;
            }

            PlayMusic(music.MusicName, fadeDuration);
        }

        public void PlayMusic(string trackName, float fadeDuration = 0f)
        {
            if (musicData == null || musicSource == null) return;

            MusicTrack track = musicData.GetTrack(trackName);
            if (track.clip == null) return;

            if (IsCurrentTrack(track)) return;

            StopAllPlayback();

            _currentTrack = track;
            _hasTrack = true;

            if (fadeDuration > 0f)
            {
                if (track.introClip != null)
                    _currentFadeCoroutine = StartCoroutine(PlayIntroThenLoop(track, fadeDuration));
                else
                    _currentFadeCoroutine = StartCoroutine(AnimateMusicCrossfade(track.clip, track.volume, true, fadeDuration));
            }
            else
            {
                PlayInstant(track);
            }
        }

        private bool IsCurrentTrack(MusicTrack track)
        {
            if (!_hasTrack) return false;

            return track.music != null
                ? track.music == _currentTrack.music
                : track.clip == _currentTrack.clip;
        }

        private void PlayInstant(MusicTrack track)
        {
            musicSource.Stop();
            musicSource.volume = track.volume;

            if (track.introClip != null)
            {
                musicSource.clip = track.introClip;
                musicSource.loop = false;
                musicSource.Play();

                _introCoroutine = StartCoroutine(WaitIntroThenLoopInstant(track));
            }
            else
            {
                musicSource.clip = track.clip;
                musicSource.loop = true;
                musicSource.Play();
            }
        }

        private IEnumerator WaitIntroThenLoopInstant(MusicTrack track)
        {
            while (musicSource.isPlaying)
                yield return null;

            musicSource.clip = track.clip;
            musicSource.loop = true;
            musicSource.volume = track.volume;
            musicSource.Play();

            _introCoroutine = null;
        }

        public void StopMusic(float fadeDuration = 0f)
        {
            if (musicSource == null) return;

            StopAllPlayback();
            _hasTrack = false;

            if (fadeDuration <= 0f)
            {
                musicSource.Stop();
                musicSource.clip = null;
                musicSource.volume = 0f;
                return;
            }

            _currentFadeCoroutine = StartCoroutine(FadeOutAndStop(fadeDuration));
        }

        private void StopAllPlayback()
        {
            if (_currentFadeCoroutine != null)
            {
                StopCoroutine(_currentFadeCoroutine);
                _currentFadeCoroutine = null;
            }

            if (_introCoroutine != null)
            {
                StopCoroutine(_introCoroutine);
                _introCoroutine = null;
            }
        }

        // ======================== INTRO + LOOP ========================

        private IEnumerator PlayIntroThenLoop(MusicTrack track, float fadeDuration)
        {
            musicSource.clip = track.introClip;
            musicSource.loop = false;
            musicSource.volume = track.volume;
            musicSource.Play();

            while (musicSource.isPlaying)
                yield return null;

            _currentFadeCoroutine = StartCoroutine(AnimateMusicCrossfade(track.clip, track.volume, true, fadeDuration));
        }

        // ======================== CROSSFADE ========================

        private IEnumerator AnimateMusicCrossfade(AudioClip nextTrack, float targetVolume, bool loop, float fadeDuration = 0.5f)
        {
            musicSource.clip = nextTrack;
            musicSource.loop = loop;
            musicSource.volume = 0f;
            musicSource.Play();

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
                musicSource.volume = Mathf.Lerp(0f, targetVolume, t);
                yield return null;
            }

            musicSource.volume = targetVolume;
            _currentFadeCoroutine = null;
        }

        // ======================== FADE OUT & STOP ========================

        private IEnumerator FadeOutAndStop(float fadeDuration)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            musicSource.Stop();
            musicSource.clip = null;
            musicSource.volume = 0f;

            _currentFadeCoroutine = null;
        }
    }
}