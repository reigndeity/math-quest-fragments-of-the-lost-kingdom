using System.Collections.Generic;
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

        /// <summary>Starts <paramref name="sceneToReload"/> over: loads a fresh copy next to it, then unloads the old one.</summary>
        public static void Reload(Scene sceneToReload)
        {
            if (isLoading || !sceneToReload.IsValid() || !sceneToReload.isLoaded)
                return;

            string sceneName = sceneToReload.name;

            // Remember the scenes that exist now, so the new copy can be told apart from the old one.
            var before = new HashSet<int>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                before.Add(SceneManager.GetSceneAt(i).handle);

            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError($"Could not reload scene '{sceneName}'. Is it in Build Settings?");
                return;
            }

            isLoading = true;
            load.completed += _ =>
            {
                isLoading = false;

                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (!before.Contains(scene.handle) && scene.name == sceneName)
                    {
                        SceneManager.SetActiveScene(scene);
                        break;
                    }
                }

                if (sceneToReload.IsValid() && sceneToReload.isLoaded)
                    SceneManager.UnloadSceneAsync(sceneToReload);
            };
        }
    }
}
