using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MazeRunner.Levels
{
    /// <summary>
    /// The level's living enemies, as a set: each enemy joins when it starts (scene-placed or spawned later)
    /// and leaves when it is defeated, so a death never counts twice and a damaged, stunned or switched-off
    /// enemy still counts. Destroyed entries (a scene reload) drop out by themselves.
    /// </summary>
    public class EnemyTracker : MonoBehaviour
    {
        private static EnemyTracker instance;
        private readonly HashSet<MonoBehaviour> alive = new HashSet<MonoBehaviour>();

        public static EnemyTracker Instance => instance;
        public bool AllEnemiesDefeated => RemainingEnemyCount == 0;

        public int RemainingEnemyCount
        {
            get
            {
                alive.RemoveWhere(e => e == null);
                return alive.Count;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateOnLoad()
        {
            if (instance == null)
            {
                GameObject trackerObject = new GameObject(nameof(EnemyTracker));
                trackerObject.AddComponent<EnemyTracker>();
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            RegisterAllEnemies();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                instance = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RegisterAllEnemies();
        }

        /// <summary>Scans the scene for enemies again (call after spawning enemies at load time).</summary>
        public void RefreshEnemyCount()
        {
            RegisterAllEnemies();
        }

        private void RegisterAllEnemies()
        {
            // Before their own Start, so nothing reads an empty level in between.
            foreach (var g in FindObjectsByType<Enemies.GoblinEnemy>(FindObjectsSortMode.None)) if (!g.IsDead) alive.Add(g);
            foreach (var w in FindObjectsByType<Enemies.WraithEnemy>(FindObjectsSortMode.None)) if (!w.IsDead) alive.Add(w);
            foreach (var e in FindObjectsByType<Enemies.Enemy>(FindObjectsSortMode.None)) if (!e.IsDefeated) alive.Add(e);
        }

        /// <summary>An enemy has started: it counts until it is defeated.</summary>
        public void RegisterEnemy(MonoBehaviour enemy)
        {
            if (enemy != null) alive.Add(enemy);
        }

        public void NotifyEnemyDefeated(MonoBehaviour enemy)
        {
            alive.Remove(enemy);
        }
    }
}
