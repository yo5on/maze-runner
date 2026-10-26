using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MazeRunner.Levels
{
    /// <summary>
    /// Tracks level unlocks and loads the configured level sequence (Level1 -> Level2 -> Level3).
    /// The single source of truth for progression, saved in PlayerPrefs:
    /// - Unlocks: which levels may be played (Level Select). Completing a level unlocks the next.
    /// - Cleared state: levels genuinely completed; older saves infer this from the next unlock.
    /// - Current level: the level Play continues - the level last entered, or the next one once a level
    ///   is completed. Quitting to the menu mid-level keeps it, so Play restarts that level from its start
    ///   (checkpoints are per run of a level and are not saved). A new player starts Level 1.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class LevelProgressionManager : MonoBehaviour
    {
        private const string UnlockKeyPrefix = "MazeRunner.LevelUnlocked.";
        private const string ClearedKeyPrefix = "MazeRunner.LevelCleared.";
        public const string CurrentLevelKey = "MazeRunner.CurrentLevel";

        /// <summary>
        /// How many levels of the sequence can be played: all three. Completing a level unlocks the
        /// next; completing the last one shows the victory screen (LevelCompleteController).
        /// </summary>
        public const int EnabledLevelCount = 3;

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
            SaveCurrentLevel(sceneIndex);
            LevelStarted?.Invoke(currentLevelIndex);
        }

        /// <summary>True for levels that are currently part of the game (see <see cref="EnabledLevelCount"/>).</summary>
        public bool IsLevelEnabled(int levelIndex)
        {
            return IsValidLevelIndex(levelIndex) && levelIndex < EnabledLevelCount;
        }

        public bool IsLevelUnlocked(int levelIndex)
        {
            if (!IsLevelEnabled(levelIndex))
            {
                return false;
            }

            return levelIndex == 0 || PlayerPrefs.GetInt(GetUnlockKey(levelIndex), 0) == 1;
        }

        public bool IsLevelCleared(int levelIndex)
        {
            if (!IsLevelEnabled(levelIndex)) return false;

            string key = GetClearedKey(levelIndex);
            if (PlayerPrefs.HasKey(key)) return PlayerPrefs.GetInt(key, 0) == 1;

            // Older saves recorded completion only by unlocking the next level.
            return levelIndex + 1 < LevelCount && IsLevelUnlocked(levelIndex + 1);
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

            SetLevelCleared(currentLevelIndex, true);
            LevelCompleted?.Invoke(currentLevelIndex);

            int nextLevelIndex = currentLevelIndex + 1;
            if (!IsLevelEnabled(nextLevelIndex))
            {
                AllLevelsCompleted?.Invoke();
            }
            else
            {
                SetLevelUnlocked(nextLevelIndex, true);
                SaveCurrentLevel(nextLevelIndex); // Play now continues with the level just unlocked
            }
        }

        /// <summary>
        /// The level Play continues: the saved current level when it is still unlocked, otherwise the
        /// furthest unlocked level; Level 1 (index 0) for a new player.
        /// </summary>
        public int ResumeLevelIndex
        {
            get
            {
                int saved = PlayerPrefs.GetInt(CurrentLevelKey, -1);
                if (IsLevelUnlocked(saved)) return saved;
                int furthest = 0;
                for (int i = 0; i < LevelCount; i++) if (IsLevelUnlocked(i)) furthest = i;
                return furthest;
            }
        }

        /// <summary>Play: loads <see cref="ResumeLevelIndex"/>, never resetting anything.</summary>
        public bool ContinueGame() => StartLevel(ResumeLevelIndex);

        private void SaveCurrentLevel(int levelIndex)
        {
            if (!IsLevelEnabled(levelIndex) || PlayerPrefs.GetInt(CurrentLevelKey, -1) == levelIndex) return;
            PlayerPrefs.SetInt(CurrentLevelKey, levelIndex);
            PlayerPrefs.Save();
        }

        public void ResetProgress()
        {
            for (int i = 0; i < LevelCount; i++)
            {
                PlayerPrefs.DeleteKey(GetUnlockKey(i));
                PlayerPrefs.DeleteKey(GetClearedKey(i));
            }
            PlayerPrefs.DeleteKey(CurrentLevelKey);

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

        private void SetLevelCleared(int levelIndex, bool cleared)
        {
            if (!IsValidLevelIndex(levelIndex)) return;
            PlayerPrefs.SetInt(GetClearedKey(levelIndex), cleared ? 1 : 0);
            PlayerPrefs.Save();
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

        private static string GetClearedKey(int levelIndex)
        {
            return ClearedKeyPrefix + levelIndex;
        }
    }
}
