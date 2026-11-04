using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MathQuest.Bootstrap
{
    [Serializable]
    public sealed class BootstrapSceneEntry
    {
#if UNITY_EDITOR
        [SerializeField] private SceneAsset scene;
#endif
        [SerializeField, HideInInspector] private string scenePath;
        [SerializeField] private bool makeActiveScene;

        public string ScenePath => scenePath;
        public bool MakeActiveScene => makeActiveScene;

#if UNITY_EDITOR
        public void SyncPath()
        {
            scenePath = scene != null ? AssetDatabase.GetAssetPath(scene) : string.Empty;
        }
#endif
    }

    /// <summary>
    /// Startup composition: the scenes the persistent Root scene loads additively, in order.
    /// Scene fields keep their project path for runtime loading.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameBootstrapConfig",
        menuName = "Math Quest/Bootstrap/Game Bootstrap Config")]
    public sealed class GameBootstrapConfig : ScriptableObject
    {
        [SerializeField] private List<BootstrapSceneEntry> scenes = new();

        public IReadOnlyList<BootstrapSceneEntry> Scenes => scenes;

#if UNITY_EDITOR
        private void OnValidate()
        {
            foreach (BootstrapSceneEntry entry in scenes)
                entry?.SyncPath();
        }
#endif
    }
}
