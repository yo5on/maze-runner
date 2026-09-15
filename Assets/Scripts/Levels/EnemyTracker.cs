    using UnityEngine;
using UnityEngine.SceneManagement;

namespace MazeRunner.Levels
{
    public class EnemyTracker : MonoBehaviour
    {
        private static EnemyTracker instance;
        private int enemyCount;
        private int defeatedCount;
        
        public static EnemyTracker Instance => instance;
        public bool AllEnemiesDefeated => enemyCount > 0 && defeatedCount >= enemyCount;
        public int RemainingEnemyCount => Mathf.Max(0, enemyCount - defeatedCount);
        
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
        
        private void RegisterAllEnemies()
        {
            enemyCount = 0;
            defeatedCount = 0;
            
            var goblins = FindObjectsByType<Enemies.GoblinEnemy>(FindObjectsSortMode.None);
            enemyCount += goblins.Length;
            
            var wraiths = FindObjectsByType<Enemies.WraithEnemy>(FindObjectsSortMode.None);
            enemyCount += wraiths.Length;
            
            var baseEnemies = FindObjectsByType<Enemies.Enemy>(FindObjectsSortMode.None);
            enemyCount += baseEnemies.Length;
        }
        
        public void NotifyEnemyDefeated()
        {
            defeatedCount++;
        }
    }
}