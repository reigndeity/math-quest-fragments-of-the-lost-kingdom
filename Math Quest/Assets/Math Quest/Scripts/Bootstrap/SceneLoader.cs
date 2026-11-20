using System.Collections;
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

        /// <summary>
        /// Same as the two-argument Replace, but with the loading transition on top: the screen is covered, the
        /// scenes are swapped, then the screen is uncovered. Pass false for an instant swap.
        /// </summary>
        public static void Replace(string targetSceneName, Scene sceneToUnload, bool withTransition)
        {
            if (!withTransition)
            {
                Replace(targetSceneName, sceneToUnload);
                return;
            }

            if (isLoading || ScreenTransition.IsRunning)
                return;

            ScreenTransition.Run(() => ReplaceRoutine(targetSceneName, sceneToUnload));
        }

        private static IEnumerator ReplaceRoutine(string targetSceneName, Scene sceneToUnload)
        {
            if (SceneManager.GetSceneByName(targetSceneName).isLoaded)
                yield break;

            AsyncOperation load = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError($"Could not load scene '{targetSceneName}'. Is it in Build Settings?");
                yield break;
            }

            isLoading = true;
            while (!load.isDone)
                yield return null;
            isLoading = false;

            Scene target = SceneManager.GetSceneByName(targetSceneName);
            if (target.IsValid())
                SceneManager.SetActiveScene(target);

            if (sceneToUnload.IsValid() && sceneToUnload.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(sceneToUnload);
                while (unload != null && !unload.isDone)
                    yield return null;
            }
        }

        /// <summary>Reload with the loading transition on top. Pass false for an instant reload.</summary>
        public static void Reload(Scene sceneToReload, bool withTransition)
        {
            if (!withTransition)
            {
                Reload(sceneToReload);
                return;
            }

            if (isLoading || ScreenTransition.IsRunning || !sceneToReload.IsValid() || !sceneToReload.isLoaded)
                return;

            ScreenTransition.Run(() => ReloadRoutine(sceneToReload));
        }

        private static IEnumerator ReloadRoutine(Scene sceneToReload)
        {
            string sceneName = sceneToReload.name;

            var before = new HashSet<int>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                before.Add(SceneManager.GetSceneAt(i).handle);

            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError($"Could not reload scene '{sceneName}'. Is it in Build Settings?");
                yield break;
            }

            isLoading = true;
            while (!load.isDone)
                yield return null;
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
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(sceneToReload);
                while (unload != null && !unload.isDone)
                    yield return null;
            }
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
