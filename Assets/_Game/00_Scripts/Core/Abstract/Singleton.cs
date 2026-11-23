using System.Collections;
using Slafurry.Core.Interface;
using Slafurry.Systems;
using UnityEngine;

namespace Slafurry.Core.Abstract
{
    public abstract class Singleton<T> : MonoBehaviour, IInitializable
        where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        public virtual int Priority => 0;

        // Sealed (not virtual) - override subclass from OnSingletonAwake(),
        // so register to LoadingManager never forgot to be called.
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this as T;
            OnSingletonAwake();
            LoadingSystem.Instance.Register(this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            OnSingletonDestroyed();
        }

        /// <summary>Awake hook for subclasses. GameSystem&lt;T&gt; uses this for DontDestroyOnLoad.</summary>
        protected abstract void OnSingletonAwake();

        /// <summary>Optional OnDestroy hook for subclasses.</summary>
        protected virtual void OnSingletonDestroyed() { }

        public abstract IEnumerator Initialize();
        public abstract void PostInitialize();
    }
}
