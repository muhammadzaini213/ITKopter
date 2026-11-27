using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Slafurry.Systems.Pause;

/// <summary>
/// Scene-bound driver behind <see cref="CombatFeel"/>. Created on demand in
/// the active scene. Also reacts to combat events (player hurt, enemy and
/// boss kills) so every hit has weight without per-prefab wiring.
/// </summary>
public class CombatFeelRunner : MonoBehaviour
{
    // ---- reaction tuning ----------------------------------------------
    private const float PlayerHurtCooldown = 0.12f;
    private static readonly Color PlayerHurtFlash = new Color(1f, 0.1f, 0.15f, 0.22f);
    private static readonly Color BossKillFlash = new Color(1f, 1f, 1f, 0.55f);

    // ---- time control (hitstop + slow-mo) ----------------------------
    private bool hitStopActive;
    private float hitStopRemaining;
    private float hitStopScale = 1f;

    private bool slowMoActive;
    private float slowMoRemaining;
    private float slowMoDuration;
    private float slowMoScale = 1f;

    private float lastAppliedScale = 1f;
    private bool ownsTime;

    // ---- letterbox (skill cut-in) ------------------------------------
    private RectTransform barTop;
    private RectTransform barBottom;
    private float letterbox;        // 0..1 current
    private float letterboxTarget;  // 0..1

    // ---- shake --------------------------------------------------------
    private float shakeStrength;
    private float shakeDuration;
    private float shakeRemaining;
    private float noiseSeed;
    private Vector3 currentOffset;
    private Vector3 appliedCameraOffset;
    private Transform fallbackCamera;
    private readonly List<CinemachineVirtualCameraBase> hookedCameras =
        new List<CinemachineVirtualCameraBase>();

    // ---- flash --------------------------------------------------------
    private Image flashImage;
    private Color flashColor;
    private float flashDuration;
    private float flashRemaining;

    private float lastPlayerHurtTime = -10f;

    public Vector3 ShakeOffset => currentOffset;

    public static CombatFeelRunner Create()
    {
        var go = new GameObject("[CombatFeel]");

        // Keep it in the active (gameplay) scene so it unloads with it.
        Scene active = SceneManager.GetActiveScene();
        if (active.IsValid() && go.scene != active)
            SceneManager.MoveGameObjectToScene(go, active);

        return go.AddComponent<CombatFeelRunner>();
    }

    private void Awake()
    {
        noiseSeed = Random.value * 100f;
        HookCameras();
    }

    private void OnEnable()
    {
        Health.OnAnyDamaged += HandleAnyDamaged;
        EnemyLifecycle.OnAnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        Health.OnAnyDamaged -= HandleAnyDamaged;
        EnemyLifecycle.OnAnyEnemyDied -= HandleEnemyDied;
    }

    private void OnDestroy()
    {
        EndHitStop();
        RestoreFallbackCamera();
    }

    // =================================================================
    // Time control: hitstop + slow-mo share one owner of Time.timeScale.
    // We only touch time while it is 1 (nobody else slowing) or still the
    // value we set — pause menus, Trady's popup and the story victory
    // freeze (all timeScale 0) always win.
    // =================================================================

    public bool IsSlowMo => slowMoActive;

    public void HitStop(float duration, float timeScale)
    {
        if (!CanTakeTime())
            return;

        timeScale = Mathf.Clamp(timeScale, 0.001f, 1f);

        if (!hitStopActive)
        {
            hitStopActive = true;
            hitStopRemaining = duration;
            hitStopScale = timeScale;
        }
        else
        {
            hitStopRemaining = Mathf.Max(hitStopRemaining, duration);
            hitStopScale = Mathf.Min(hitStopScale, timeScale);
        }

        ApplyTime();
    }

    /// <summary>
    /// Dramatic slow-motion: holds <paramref name="timeScale"/> then eases
    /// back to normal over the last 40% (real-time seconds).
    /// </summary>
    public void SlowMo(float duration, float timeScale, bool letterboxBars)
    {
        if (!CanTakeTime())
            return;

        slowMoActive = true;
        slowMoDuration = Mathf.Max(0.05f, duration);
        slowMoRemaining = slowMoDuration;
        slowMoScale = Mathf.Clamp(timeScale, 0.02f, 1f);

        if (letterboxBars)
        {
            EnsureOverlay();
            letterboxTarget = 1f;
        }

        ApplyTime();
    }

    private bool CanTakeTime()
    {
        if (IsPaused())
            return false;

        return ownsTime
            ? Mathf.Approximately(Time.timeScale, lastAppliedScale) || Time.timeScale >= 0.999f
            : Time.timeScale >= 0.999f;
    }

    private float DesiredScale()
    {
        float scale = 1f;

        if (slowMoActive)
        {
            float t = slowMoDuration > 0f ? slowMoRemaining / slowMoDuration : 0f; // 1 → 0
            float ease = t > 0.4f ? 1f : t / 0.4f;                                  // hold, then ease out
            scale = Mathf.Lerp(1f, slowMoScale, ease * ease);
        }

        if (hitStopActive)
            scale = Mathf.Min(scale, hitStopScale);

        return scale;
    }

    private void ApplyTime()
    {
        if (IsPaused())
            return;

        bool timeIsOurs = Time.timeScale >= 0.999f ||
                          (ownsTime && Mathf.Approximately(Time.timeScale, lastAppliedScale));

        if (!timeIsOurs)
            return;

        float desired = DesiredScale();
        Time.timeScale = desired;
        lastAppliedScale = desired;
        ownsTime = desired < 0.999f;
    }

    private void UpdateTime()
    {
        float dt = Time.unscaledDeltaTime;

        if (hitStopActive)
        {
            hitStopRemaining -= dt;
            if (hitStopRemaining <= 0f)
                hitStopActive = false;
        }

        if (slowMoActive)
        {
            slowMoRemaining -= dt;
            if (slowMoRemaining <= 0f)
            {
                slowMoActive = false;
                letterboxTarget = 0f;
            }
        }

        if (hitStopActive || slowMoActive || ownsTime)
            ApplyTime();

        // Paused externally: drop our claim; resume sets time back to 1.
        if (IsPaused())
            ownsTime = false;
    }

    private void EndHitStop()
    {
        hitStopActive = false;
        slowMoActive = false;

        if (ownsTime && Mathf.Approximately(Time.timeScale, lastAppliedScale) && !IsPaused())
            Time.timeScale = 1f;

        ownsTime = false;
    }

    private static bool IsPaused()
    {
        return PauseSystem.Instance != null && PauseSystem.Instance.IsPaused;
    }

    // =================================================================
    // Shake
    // =================================================================

    public void Shake(float strength, float duration)
    {
        HookCameras();

        float currentStrength = shakeDuration > 0f
            ? shakeStrength * Mathf.Clamp01(shakeRemaining / shakeDuration)
            : 0f;

        if (strength < currentStrength)
        {
            // Weaker request: just extend a bit so rapid hits keep rumbling.
            shakeRemaining = Mathf.Max(shakeRemaining, duration * 0.5f);
            return;
        }

        shakeStrength = Mathf.Min(strength, 1.2f);
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeRemaining = shakeDuration;
    }

    private void UpdateShake()
    {
        if (shakeRemaining <= 0f)
        {
            currentOffset = Vector3.zero;
            return;
        }

        // Shake keeps running through hitstop (unscaled) — feels punchier.
        shakeRemaining = Mathf.Max(0f, shakeRemaining - Time.unscaledDeltaTime);

        float t = shakeDuration > 0f ? shakeRemaining / shakeDuration : 0f;
        float amount = shakeStrength * t * t;
        float time = Time.unscaledTime * 32f;

        currentOffset = new Vector3(
            (Mathf.PerlinNoise(noiseSeed, time) - 0.5f) * 2f * amount,
            (Mathf.PerlinNoise(noiseSeed + 37.1f, time) - 0.5f) * 2f * amount,
            0f);
    }

    private void HookCameras()
    {
        hookedCameras.RemoveAll(c => c == null);

        var cameras = FindObjectsByType<CinemachineVirtualCameraBase>(FindObjectsSortMode.None);

        foreach (var vcam in cameras)
        {
            if (vcam == null || hookedCameras.Contains(vcam))
                continue;

            if (vcam.GetComponent<CombatFeelShakeExtension>() == null)
                vcam.gameObject.AddComponent<CombatFeelShakeExtension>();

            hookedCameras.Add(vcam);
        }

        if (hookedCameras.Count == 0 && fallbackCamera == null && Camera.main != null)
        {
            // No Cinemachine — offset the camera transform directly.
            if (Camera.main.GetComponent<CinemachineBrain>() == null)
                fallbackCamera = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (fallbackCamera == null)
            return;

        fallbackCamera.localPosition += currentOffset - appliedCameraOffset;
        appliedCameraOffset = currentOffset;
    }

    private void RestoreFallbackCamera()
    {
        if (fallbackCamera != null)
            fallbackCamera.localPosition -= appliedCameraOffset;

        appliedCameraOffset = Vector3.zero;
    }

    // =================================================================
    // Flash
    // =================================================================

    public void Flash(Color color, float duration)
    {
        EnsureOverlay();

        // Stronger flash wins; weaker one is ignored while a flash is live.
        float liveAlpha = flashRemaining > 0f
            ? flashColor.a * (flashRemaining / flashDuration)
            : 0f;

        if (color.a < liveAlpha)
            return;

        flashColor = color;
        flashDuration = duration;
        flashRemaining = duration;
        flashImage.color = color;
        flashImage.enabled = true;
    }

    private void EnsureOverlay()
    {
        if (flashImage != null)
            return;

        var canvasGo = new GameObject("CombatFeelFlash");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;

        var imageGo = new GameObject("Flash");
        imageGo.transform.SetParent(canvasGo.transform, false);

        flashImage = imageGo.AddComponent<Image>();
        flashImage.raycastTarget = false;
        flashImage.color = Color.clear;
        flashImage.enabled = false;

        RectTransform rect = flashImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        barTop = CreateBar(canvasGo.transform, "LetterboxTop", top: true);
        barBottom = CreateBar(canvasGo.transform, "LetterboxBottom", top: false);
        ApplyLetterbox();
    }

    private static RectTransform CreateBar(Transform parent, string name, bool top)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.raycastTarget = false;
        image.color = new Color(0f, 0f, 0f, 0.85f);

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rect.sizeDelta = Vector2.zero;
        return rect;
    }

    private void UpdateLetterbox()
    {
        if (barTop == null)
            return;

        float speed = letterboxTarget > letterbox ? 9f : 4f;
        float next = Mathf.MoveTowards(letterbox, letterboxTarget, Time.unscaledDeltaTime * speed);

        if (Mathf.Approximately(next, letterbox))
            return;

        letterbox = next;
        ApplyLetterbox();
    }

    private void ApplyLetterbox()
    {
        if (barTop == null)
            return;

        // Bars cover 9% of the screen height each at full strength.
        float height = Screen.height * 0.09f * letterbox;
        var canvas = barTop.GetComponentInParent<Canvas>();
        float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;

        barTop.sizeDelta = new Vector2(0f, height / scale);
        barBottom.sizeDelta = new Vector2(0f, height / scale);
        barTop.gameObject.SetActive(letterbox > 0.001f);
        barBottom.gameObject.SetActive(letterbox > 0.001f);
    }

    private void UpdateFlash()
    {
        if (flashImage == null || flashRemaining <= 0f)
            return;

        flashRemaining = Mathf.Max(0f, flashRemaining - Time.unscaledDeltaTime);

        Color c = flashColor;
        c.a = flashColor.a * (flashRemaining / flashDuration);
        flashImage.color = c;

        if (flashRemaining <= 0f)
            flashImage.enabled = false;
    }

    // =================================================================
    // Update
    // =================================================================

    private void Update()
    {
        UpdateTime();
        UpdateShake();
        UpdateFlash();
        UpdateLetterbox();
    }

    // =================================================================
    // Combat reactions
    // =================================================================

    private void HandleAnyDamaged(Health health, float amount)
    {
        if (health == null || amount <= 0f)
            return;

        if (health.GetComponent<Character>() == null)
        {
            // Enemy: only heavy single hits (EX skills, matchups) rumble —
            // regular bullet streams would turn into constant noise.
            if (health.MaxHealth > 0f && amount >= health.MaxHealth * 0.2f)
                CombatFeel.Shake(0.08f, 0.12f);

            return;
        }

        if (Time.unscaledTime - lastPlayerHurtTime < PlayerHurtCooldown)
            return;

        lastPlayerHurtTime = Time.unscaledTime;

        float fraction = health.MaxHealth > 0f ? amount / health.MaxHealth : 0.1f;

        CombatFeel.HitStop(Mathf.Lerp(0.05f, 0.12f, Mathf.Clamp01(fraction * 4f)));
        CombatFeel.Shake(Mathf.Lerp(0.14f, 0.38f, Mathf.Clamp01(fraction * 4f)), 0.22f);
        CombatFeel.Flash(PlayerHurtFlash, 0.18f);
    }

    private void HandleEnemyDied(EnemyLifecycle enemy)
    {
        if (enemy == null)
            return;

        if (enemy.GetComponent<EnemyBoss>() != null)
        {
            CombatFeel.HitStop(0.3f, 0.01f);
            CombatFeel.Shake(0.55f, 0.7f);
            CombatFeel.Flash(BossKillFlash, 0.35f);
            SkillVFX.Ring(enemy.transform.position, Color.white, 0.3f, 6f, 0.6f, 0.25f);
            SkillVFX.Sparks(enemy.transform.position, Color.white, 40, 9f, 0.7f);
            return;
        }

        CombatFeel.HitStop(0.035f);
        CombatFeel.Shake(0.1f, 0.14f);
        SkillVFX.Ring(enemy.transform.position, new Color(1f, 0.85f, 0.5f, 0.9f), 0.2f, 1.4f, 0.25f, 0.08f);
    }
}
