using UnityEngine;
using UnityEngine.UI;
using MazeRunner.Audio;

namespace MazeRunner.UI
{
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(PlaySound);
        }
        
        private void PlaySound()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClick();
            }
        }
    }
}