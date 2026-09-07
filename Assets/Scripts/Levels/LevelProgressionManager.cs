using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MazeRunner.Levels
{
    /// <summary>
    /// Tracks level unlocks and loads the configured level sequence.
    /// The default sequence uses TestLevel as Level 1.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class LevelProgressionManager : MonoBehaviour
    {
        private const string UnlockKeyPrefix = "MazeRunner.LevelUnlocked.";

        private static LevelProgressionManager instance;

        [Header("Level Sequence")]
        [SerializeField] private string[] levelSceneNames = { "Level1", "Level2", "Level3" };

        [Header("Progression")]
        [SerializeField] private bool persistProgress = true;
        [SerializeField] private bool unlockFirstLevelOnStart = true;

        private int currentLevelIndex = -1;
        private bool isTransitioning;

        public static LevelProgressionManager Instance => instance;
        public int CurrentLevelIndex => currentLevelIndex;
        public int CurrentLevelNumber => currentLevelIndex + 1;
        public int LevelCount => levelSceneNames?.Length ?? 0;
        public bool IsTransitioning => isTransitioning;

        public event Action<int> LevelStarted;
        public event Action<int> LevelCompleted;
        public event Action AllLevelsCompleted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateOnLoad()
        {
            if (instance == null)
            {
                GameObject managerObject = new GameObject(nameof(LevelProgressionManager));
                managerObject.AddComponent<LevelProgressionManager>();
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

            if (persistProgress)
            {
                DontDestroyOnLoad(gameObject);
            }

            SceneManager.sceneLoaded += OnSceneLoaded;

            if (unlockFirstLevelOnStart && LevelCount > 0 && !PlayerPrefs.HasKey(GetUnlockKey(0)))
            {
                SetLevelUnlocked(0, true);
            }
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            int sceneIndex = GetLevelIndex(scene.name);
            if (sceneIndex < 0)
            {
                return;
            }

            currentLevelIndex = sceneIndex;
            isTransitioning = false;
            LevelStarted?.Invoke(currentLevelIndex);
        }

        public bool IsLevelUnlocked(int levelIndex)
        {
            if (!IsValidLevelIndex(levelIndex))
            {
                return false;
            }

            return levelIndex == 0 || PlayerPrefs.GetInt(GetUnlockKey(levelIndex), 0) == 1;
        }

        public bool StartLevel(int levelIndex)
        {
            if (!IsValidLevelIndex(levelIndex) || !IsLevelUnlocked(levelIndex))
            {
                return false;
            }

            return LoadLevel(levelIndex);
        }

        public void CompleteCurrentLevel()
        {
            if (currentLevelIndex < 0)
            {
                currentLevelIndex = GetLevelIndex(SceneManager.GetActiveScene().name);
            }

            if (currentLevelIndex < 0 || isTransitioning)
            {
                return;
            }

            LevelCompleted?.Invoke(currentLevelIndex);

            int nextLevelIndex = currentLevelIndex + 1;
            if (!IsValidLevelIndex(nextLevelIndex))
            {
                AllLevelsCompleted?.Invoke();
            }
            else
            {
                SetLevelUnlocked(nextLevelIndex, true);
            }
        }

        public void ResetProgress()
        {
            for (int i = 0; i < LevelCount; i++)
            {
                PlayerPrefs.DeleteKey(GetUnlockKey(i));
            }

            if (unlockFirstLevelOnStart && LevelCount > 0)
            {
                SetLevelUnlocked(0, true);
            }

            PlayerPrefs.Save();
        }

        public string GetLevelSceneName(int levelIndex)
        {
            return IsValidLevelIndex(levelIndex) ? levelSceneNames[levelIndex] : string.Empty;
        }

        private bool LoadLevel(int levelIndex)
        {
            string sceneName = GetLevelSceneName(levelIndex);
            if (string.IsNullOrEmpty(sceneName))
            {
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"Cannot load level '{sceneName}'. Add the scene to Build Settings.",
                    this);
                return false;
            }

            isTransitioning = true;
            SceneManager.LoadSceneAsync(sceneName);
            return true;
        }

        private void SetLevelUnlocked(int levelIndex, bool unlocked)
        {
            if (IsValidLevelIndex(levelIndex))
            {
                PlayerPrefs.SetInt(GetUnlockKey(levelIndex), unlocked ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private int GetLevelIndex(string sceneName)
        {
            if (levelSceneNames == null)
            {
                return -1;
            }

            for (int i = 0; i < levelSceneNames.Length; i++)
            {
                if (string.Equals(levelSceneNames[i], sceneName, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsValidLevelIndex(int levelIndex)
        {
            return levelSceneNames != null &&
                   levelIndex >= 0 &&
                   levelIndex < levelSceneNames.Length &&
                   !string.IsNullOrWhiteSpace(levelSceneNames[levelIndex]);
        }

        private static string GetUnlockKey(int levelIndex)
        {
            return UnlockKeyPrefix + levelIndex;
        }
    }
}