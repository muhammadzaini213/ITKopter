using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Slafurry.Systems.Audio
{
    public class SFXPlayer : MonoBehaviour
    {
        [SerializeField] private SFXData sfxData;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;

        private class CategoryPool
        {
            public AudioSource[] Sources;
            public int OldestIndex;
            public readonly Dictionary<AudioSource, string> Playing = new();
            public readonly Dictionary<AudioSource, Coroutine> FadeCoroutines = new();
            public readonly Dictionary<AudioSource, int> PlayTokens = new();
        }

        private readonly Dictionary<string, CategoryPool> _pools = new();

        void Awake()
        {
            InitPools();
        }

        private void InitPools()
        {
            if (sfxData?.categories == null) return;

            foreach (var category in sfxData.categories)
            {
                var pool = new CategoryPool
                {
                    Sources = new AudioSource[category.poolSize]
                };

                for (int i = 0; i < category.poolSize; i++)
                {
                    GameObject go = new GameObject($"SFX_{category.categoryName}_{i}");
                    go.transform.SetParent(transform);

                    AudioSource source = go.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.outputAudioMixerGroup = sfxMixerGroup;

                    pool.Sources[i] = source;
                }

                _pools[category.categoryName] = pool;
            }
        }

        private void StopFadeIfAny(CategoryPool pool, AudioSource source)
        {
            if (pool.FadeCoroutines.TryGetValue(source, out var co) && co != null)
            {
                StopCoroutine(co);
            }
            pool.FadeCoroutines.Remove(source);
        }

        private AudioSource GetAvailableSource(CategoryPool pool)
        {
            // 1) find a source that is actually idle
            foreach (var source in pool.Sources)
            {
                if (!source.isPlaying)
                {
                    StopFadeIfAny(pool, source);
                    pool.Playing.Remove(source);
                    source.loop = false;
                    return source;
                }
            }

            // 2) all busy -> prefer stealing a source that is NOT fading,
            //    so a fade-out in progress isn't cut off halfway.
            //    A fading source is only stolen as a last resort (step 3).
            AudioSource nonFadingCandidate = null;
            foreach (var source in pool.Sources)
            {
                if (!pool.FadeCoroutines.ContainsKey(source))
                {
                    nonFadingCandidate = source;
                    break;
                }
            }

            // 3) fallback: every source is fading too -> fall back to the old round-robin
            AudioSource stolen = nonFadingCandidate != null ? nonFadingCandidate : pool.Sources[pool.OldestIndex];

            StopFadeIfAny(pool, stolen);
            stolen.Stop();
            stolen.loop = false;
            pool.Playing.Remove(stolen);

            pool.OldestIndex = (pool.OldestIndex + 1) % pool.Sources.Length;

            return stolen;
        }

        private int CountPlaying(CategoryPool pool, string effectName)
        {
            int count = 0;
            foreach (var source in pool.Sources)
            {
                if (!source.isPlaying)
                {
                    pool.Playing.Remove(source);
                    continue;
                }

                if (pool.Playing.TryGetValue(source, out string currentName) && currentName == effectName)
                    count++;
            }
            return count;
        }

        // ========================= FADE CORE =========================
        // Interpolation happens in decibels (dB), not linearly.
        // This matters because SFXEffect.volume can be > 1 (boost/gain, even clipping) —
        // a linear/quadratic fade in amplitude "sticks" while the signal is clipping,
        // so it feels like it stalls halfway. A fade in dB stays smooth
        // whatever the target volume is.
        private const float MinDecibel = -80f;

        private static float LinearToDecibel(float linear)
        {
            return linear > 0.0001f ? Mathf.Log10(linear) * 20f : MinDecibel;
        }

        private static float DecibelToLinear(float db)
        {
            return Mathf.Pow(10f, db / 20f);
        }

        private IEnumerator FadeRoutine(CategoryPool pool, AudioSource source, float targetVolume, float duration, bool stopWhenDone)
        {
            float startDb = LinearToDecibel(source.volume);
            float targetDb = LinearToDecibel(targetVolume);
            float t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float linear = Mathf.Clamp01(t / duration);
                float db = Mathf.Lerp(startDb, targetDb, linear);
                source.volume = DecibelToLinear(db);
                yield return null;
            }

            source.volume = targetVolume;

            if (stopWhenDone)
            {
                source.Stop();
                pool.Playing.Remove(source);
            }

            pool.FadeCoroutines.Remove(source);
        }

        // ========================= 3D =========================
        // fadeIn < 0  => use the default from SFXEffect.fadeIn
        // Volume automatically follows the distance to the AudioListener using
        // minDistance/maxDistance/rolloffMode on SFXEffect. If
        // effect.distanceOnlyNoPanning is true, the sound won't pan
        // left/right (spread 360°) — right for 2D/top-down games that only
        // need "quieter the further away", not full 3D audio.
        public void PlaySFX3D(string categoryName, string effectName, Vector3 pos, bool loop = false, float fadeIn = -1f)
        {
            if (!TryPrepare(categoryName, effectName, out var pool, out var effect, out var clip))
                return;

            AudioSource source = GetAvailableSource(pool);
            source.transform.position = pos;
            source.spatialBlend = 1f;
            source.loop = loop;
            source.clip = clip;
            source.rolloffMode = effect.rolloffMode;
            source.minDistance = effect.minDistance;
            source.maxDistance = effect.maxDistance;
            source.spread = effect.distanceOnlyNoPanning ? 360f : 0f;
            pool.Playing[source] = effectName;

            float resolvedFadeIn = fadeIn < 0f ? effect.fadeIn : fadeIn;
            StartPlaybackWithFade(pool, source, effect.volume, resolvedFadeIn);
            ScheduleAutoTailFade(pool, source, clip, effect.fadeOut);
        }

        // ========================= 2D =========================
        // fadeIn < 0  => use the default from SFXEffect.fadeIn
        public void PlaySFX2D(string categoryName, string effectName, bool loop = false, float fadeIn = -1f)
        {
            if (!TryPrepare(categoryName, effectName, out var pool, out var effect, out var clip))
                return;

            AudioSource source = GetAvailableSource(pool);
            source.transform.position = Vector3.zero;
            source.spatialBlend = 0f;
            source.loop = loop;
            source.clip = clip;
            pool.Playing[source] = effectName;

            float resolvedFadeIn = fadeIn < 0f ? effect.fadeIn : fadeIn;
            StartPlaybackWithFade(pool, source, effect.volume, resolvedFadeIn);
            ScheduleAutoTailFade(pool, source, clip, effect.fadeOut);
        }

        private void StartPlaybackWithFade(CategoryPool pool, AudioSource source, float targetVolume, float fadeIn)
        {
            // Every time this source starts a new play, invalidate any auto tail-fade
            // from the previous play so it doesn't wrongly fade out the new audio.
            pool.PlayTokens[source] = pool.PlayTokens.TryGetValue(source, out var v) ? v + 1 : 1;

            if (fadeIn > 0f)
            {
                source.volume = 0f;
                source.Play();
                pool.FadeCoroutines[source] = StartCoroutine(FadeRoutine(pool, source, targetVolume, fadeIn, stopWhenDone: false));
            }
            else
            {
                source.volume = targetVolume;
                source.Play();
            }
        }

        // Schedule an automatic fade-out at the tail of the clip (no manual Stop call needed).
        // This fixes the click at the end of clips that used to play out with no fade
        // because the caller forgot to / never called StopSFX.
        private void ScheduleAutoTailFade(CategoryPool pool, AudioSource source, AudioClip clip, float fadeOutDuration)
        {
            if (fadeOutDuration <= 0f || clip == null || source.loop)
                return;

            int token = pool.PlayTokens.TryGetValue(source, out var v) ? v : 0;

            float pitch = Mathf.Max(Mathf.Abs(source.pitch), 0.01f);
            float delay = Mathf.Max((clip.length / pitch) - fadeOutDuration, 0f);

            StartCoroutine(AutoTailFadeRoutine(pool, source, token, delay, fadeOutDuration));
        }

        private IEnumerator AutoTailFadeRoutine(CategoryPool pool, AudioSource source, int token, float delay, float fadeOutDuration)
        {
            yield return new WaitForSeconds(delay);

            // If the source was reused for another play before this fired, cancel.
            if (!pool.PlayTokens.TryGetValue(source, out var currentToken) || currentToken != token)
                yield break;

            if (source.isPlaying)
                StopSourceWithFade(pool, source, fadeOutDuration);
        }

        // ========================= STOP =========================
        // fadeOut < 0  => use the per-effect default from SFXEffect.fadeOut
        public void StopAllSFX(float fadeOut = -1f)
        {
            foreach (var kvp in _pools)
            {
                string categoryName = kvp.Key;
                CategoryPool pool = kvp.Value;

                foreach (var source in pool.Sources)
                {
                    if (!source.isPlaying)
                        continue;

                    float resolvedFadeOut = ResolveFadeOut(categoryName, pool, source, fadeOut);
                    StopSourceWithFade(pool, source, resolvedFadeOut);
                }
            }
        }

        public void StopCategory(string categoryName, float fadeOut = -1f)
        {
            if (!_pools.TryGetValue(categoryName, out var pool))
                return;

            foreach (var source in pool.Sources)
            {
                if (!source.isPlaying)
                    continue;

                float resolvedFadeOut = ResolveFadeOut(categoryName, pool, source, fadeOut);
                StopSourceWithFade(pool, source, resolvedFadeOut);
            }
        }

        public void StopSFX(string categoryName, string effectName, float fadeOut = -1f)
        {
            if (!_pools.TryGetValue(categoryName, out var pool))
                return;

            float resolvedFadeOut = fadeOut;
            if (resolvedFadeOut < 0f)
            {
                var effect = sfxData.GetSFXEffect(categoryName, effectName);
                resolvedFadeOut = effect?.fadeOut ?? 0f;
            }

            foreach (var source in pool.Sources)
            {
                if (!source.isPlaying)
                    continue;

                if (pool.Playing.TryGetValue(source, out string currentName) &&
                    currentName == effectName)
                {
                    StopSourceWithFade(pool, source, resolvedFadeOut);
                }
            }
        }

        private float ResolveFadeOut(string categoryName, CategoryPool pool, AudioSource source, float fadeOut)
        {
            if (fadeOut >= 0f)
                return fadeOut;

            if (pool.Playing.TryGetValue(source, out string effectName))
            {
                var effect = sfxData.GetSFXEffect(categoryName, effectName);
                if (effect != null)
                    return effect.fadeOut;
            }

            return 0f;
        }

        private void StopSourceWithFade(CategoryPool pool, AudioSource source, float fadeOut)
        {
            if (!source.isPlaying)
                return;

            StopFadeIfAny(pool, source);

            // If the clip doesn't loop, don't ask for a fade longer than what's left of the clip —
            // otherwise the audio runs out before the fade reaches 0,
            // and it sounds abruptly cut off instead of faded.
            if (fadeOut > 0f && !source.loop && source.clip != null)
            {
                float pitch = Mathf.Max(Mathf.Abs(source.pitch), 0.01f);
                float remaining = Mathf.Max((source.clip.length - source.time) / pitch, 0f);
                fadeOut = Mathf.Min(fadeOut, remaining);
            }

            if (fadeOut > 0f)
            {
                pool.FadeCoroutines[source] = StartCoroutine(FadeRoutine(pool, source, 0f, fadeOut, stopWhenDone: true));
            }
            else
            {
                source.Stop();
                pool.Playing.Remove(source);
            }
        }

        // ========================= Shared lookup =========================
        private bool TryPrepare(string categoryName, string effectName, out CategoryPool pool, out SFXEffect effect, out AudioClip clip)
        {
            pool = null;
            effect = null;
            clip = null;

            if (!_pools.TryGetValue(categoryName, out pool))
            {
                Debug.LogWarning($"[SFXPlayer] Category '{categoryName}' not found.");
                return false;
            }

            effect = sfxData.GetSFXEffect(categoryName, effectName);
            if (effect == null || effect.clips == null || effect.clips.Length == 0)
            {
                Debug.LogWarning($"[SFXPlayer] SFX '{categoryName}/{effectName}' not found.");
                return false;
            }

            if (CountPlaying(pool, effectName) >= effect.maxSimultaneous)
                return false;

            clip = effect.clips[Random.Range(0, effect.clips.Length)];
            return true;
        }
    }
}