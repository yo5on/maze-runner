using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MazeRunner.Levels
{
    /// <summary>
    /// Extra enemies placed in a level when it loads. A table lives in Resources/LevelEnemies/ and is named
    /// after its scene (e.g. Level1). The level scenes themselves stay unchanged; to move or remove an
    /// enemy, edit the table asset. Spawned enemies count towards the level goal like scene-placed ones.
    /// </summary>
    [CreateAssetMenu(fileName = "Level1", menuName = "Maze Runner/Level Enemy Spawn Table")]
    public class LevelEnemySpawnTable : ScriptableObject
    {
        public const string ResourceFolder = "LevelEnemies";

        [Serializable]
        public class Spawn
        {
            public string label;
            public GameObject prefab;
            public Vector2 position;
            [Tooltip("1 = start walking right, -1 = left.")]
            public int startDirection = 1;
            [Tooltip("Half-width of the patrol around the spawn point (clamped to the platform).")]
            public float patrolDistance = 1f;
        }

        [SerializeField] private Spawn[] spawns = new Spawn[0];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var table = Resources.Load<LevelEnemySpawnTable>(ResourceFolder + "/" + scene.name);
            if (table == null) return;

            int spawned = 0;
            foreach (Spawn spawn in table.spawns)
            {
                if (spawn == null || spawn.prefab == null) continue;

                GameObject enemy = Instantiate(spawn.prefab, new Vector3(spawn.position.x, spawn.position.y, 0f), Quaternion.identity);
                SceneManager.MoveGameObjectToScene(enemy, scene);
                if (!string.IsNullOrEmpty(spawn.label)) enemy.name = spawn.label;

                var goblin = enemy.GetComponent<Enemies.GoblinEnemy>();
                if (goblin != null) goblin.ConfigurePatrol(spawn.startDirection, spawn.patrolDistance);
                spawned++;
            }

            if (spawned > 0 && EnemyTracker.Instance != null) EnemyTracker.Instance.RefreshEnemyCount();
        }
    }
}
