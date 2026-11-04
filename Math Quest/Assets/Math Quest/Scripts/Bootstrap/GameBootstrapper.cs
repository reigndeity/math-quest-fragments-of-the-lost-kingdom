using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathQuest.Bootstrap
{
    /// <summary>
    /// Lives on the persistent Root scene. Loads the configured scenes additively, in order.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private GameBootstrapConfig config;
        [SerializeField] private bool beginAutomatically = true;

        private static GameBootstrapper instance;
        private bool hasStarted;

        public static GameBootstrapper Instance => instance;
        public bool IsReady { get; private set; }
        public bool IsLoading => hasStarted && !IsReady;

        public event Action BootstrapCompleted;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(transform.root.gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }

        private void Start()
        {
            if (beginAutomatically)
                BeginLoading();
        }

        public void BeginLoading()
        {
            if (hasStarted)
                return;

            if (config == null)
            {
                Debug.LogError("GameBootstrapper needs a Game Bootstrap Config.", this);
                return;
            }

            hasStarted = true;
            StartCoroutine(LoadConfiguredScenes());
        }

        private IEnumerator LoadConfiguredScenes()
        {
            foreach (BootstrapSceneEntry entry in config.Scenes)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.ScenePath))
                    continue;

                Scene scene = SceneManager.GetSceneByPath(entry.ScenePath);
                if (!scene.isLoaded)
                {
                    if (!Application.CanStreamedLevelBeLoaded(entry.ScenePath))
                    {
                        Debug.LogError(
                            $"Bootstrap scene is missing from Build Settings: {entry.ScenePath}",
                            this);
                        hasStarted = false;
                        yield break;
                    }

                    AsyncOperation operation = SceneManager.LoadSceneAsync(
                        entry.ScenePath,
                        LoadSceneMode.Additive);
                    if (operation == null)
                    {
                        Debug.LogError($"Could not begin loading scene: {entry.ScenePath}", this);
                        hasStarted = false;
                        yield break;
                    }

                    yield return operation;
                    scene = SceneManager.GetSceneByPath(entry.ScenePath);
                }

                if (entry.MakeActiveScene && scene.IsValid() && scene.isLoaded)
                    SceneManager.SetActiveScene(scene);
            }

            IsReady = true;
            BootstrapCompleted?.Invoke();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
