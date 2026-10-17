using UnityEngine;
using MazeRunner.Audio;
using MazeRunner.Player;

namespace MazeRunner.UI
{
    /// <summary>Shows a generated game-over panel when the player runs out of respawns.</summary>
    public class GeneratedGameOverPanel : MonoBehaviour
    {
        private PlayerHealth playerHealth;

        public void Bind(PlayerHealth health)
        {
            playerHealth = health;
            playerHealth.OnGameOver += Show;
        }

        private void OnDestroy()
        {
            if (playerHealth != null) playerHealth.OnGameOver -= Show;
        }

        private void Show()
        {
            gameObject.SetActive(true);
            if (AudioManager.Instance != null) AudioManager.Instance.PauseMusic();
        }
    }
}
