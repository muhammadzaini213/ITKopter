using System.Collections;
using Slafurry.Systems.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace Slafurry.Systems.Bootstrap
{
    /// <summary>
    /// Lives in the Bootstrap scene (the first scene in Build Settings).
    /// Its job: make sure every GameSystem<T> (SceneLoader, AudioSystem, etc.)
    /// has finished Initialize() + PostInitialize() before moving to the target scene.
    /// </summary>
    public class BootstrapLoader : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Scene to load once all systems are ready.")]
        [SerializeField] private SceneReference targetSceneName;

        [Header("Systems To Wait For")]
        [Tooltip("References to every GameSystem that must be ready before changing scene. " +
                 "Drag the components here (SceneLoader, AudioSystem, etc.).")]
        [SerializeField] private MonoBehaviour[] systemsToWaitFor;

        [Header("Optional")]
        [Tooltip("Extra delay (seconds) after all systems are ready, before loading the scene. Useful for a minimum splash screen duration.")]
        [SerializeField] private float minimumDelay = 0f;

        [Header("Event")]
        [SerializeField] private UnityEvent onBootstrapCompleted;

        private void Start()
        {
            StartCoroutine(BootstrapRoutine());
        }

        private IEnumerator BootstrapRoutine()
        {
            // Wait 1 frame so every other system's Awake()/OnSingletonAwake()
            // (set earlier via Script Execution Order) finishes first.
            yield return null;

            // Inisialisasi setiap system secara berurutan.
            foreach (var systemObj in systemsToWaitFor)
            {
                if (systemObj == null)
                {
                    Debug.LogWarning("[BootstrapLoader] A system slot in the Inspector is empty, skipped.");
                    continue;
                }

                if (systemObj is IGameSystemLifecycle lifecycle)
                {
                    yield return StartCoroutine(lifecycle.Initialize());
                    lifecycle.PostInitialize();
                }
                else
                {
                    Debug.LogWarning($"[BootstrapLoader] {systemObj.GetType().Name} is not a GameSystem, skipped from the lifecycle wait.");
                }
            }

            if (minimumDelay > 0f)
                yield return new WaitForSecondsRealtime(minimumDelay);

            Debug.Log($"[BootstrapLoader] All systems ready. Moving to scene '{targetSceneName}'.");

            if (targetSceneName == null)
            {
                Debug.LogError("[BootstrapLoader] targetSceneName is empty! Set it in the Inspector.");
                yield break;
            }

            onBootstrapCompleted?.Invoke();
        }
        public void ChangeScene()
        {
            SceneSystem.Load(targetSceneName);
        }
    }


    /// <summary>
    /// Helper interface so BootstrapLoader can call Initialize()/PostInitialize()
    /// without knowing the generic type T of GameSystem<T>.
    /// Implement this on GameSystem<T> (see the note below).
    /// </summary>
    public interface IGameSystemLifecycle
    {
        IEnumerator Initialize();
        void PostInitialize();
    }
}