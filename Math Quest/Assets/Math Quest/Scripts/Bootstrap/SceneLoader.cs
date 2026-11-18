using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathQuest.Bootstrap
{
    /// <summary>
    /// Swaps one additive scene for another (for example Menu for Battle) without touching the persistent
    /// Root scene. The new scene must be in Build Settings.
    /// </summary>
    public static class SceneLoader
    {
        private static bool isLoading;

        /// <summary>Loads <paramref name="targetSceneName"/> additively, makes it active, then unloads <paramref name="sceneToUnload"/>.</summary>
        public static void Replace(string targetSceneName, Scene sceneToUnload)
        {
            if (isLoading)
                return;

            if (SceneManager.GetSceneByName(targetSceneName).isLoaded)
                return;

            AsyncOperation load = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError($"Could not load scene '{targetSceneName}'. Is it in Build Settings?");
                return;
            }

            isLoading = true;
            load.completed += _ =>
            {
                isLoading = false;

                Scene target = SceneManager.GetSceneByName(targetSceneName);
                if (target.IsValid())
                    SceneManager.SetActiveScene(target);

                if (sceneToUnload.IsValid() && sceneToUnload.isLoaded)
                    SceneManager.UnloadSceneAsync(sceneToUnload);
            };
        }
    }
}
