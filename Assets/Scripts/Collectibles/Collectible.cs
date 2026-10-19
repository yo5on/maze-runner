using UnityEngine;
using System;

namespace MazeRunner.Collectibles
{
    public enum CollectibleType { Coin, Gem, Health }
    
    [RequireComponent(typeof(Collider2D))]
    public class Collectible : MonoBehaviour
    {
        [SerializeField] private CollectibleType type = CollectibleType.Coin;
        [SerializeField] private int value = 1;
        
        public static event Action<CollectibleType, int> OnCollected;
        
        private const float PopTime = 0.18f;
        
        private bool collected;
        private Vector3 restPosition;
        private float bobPhase;
        
        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            restPosition = transform.localPosition;
            bobPhase = transform.position.x; // neighbouring pickups do not bob in step
        }
        
        private void Update()
        {
            // A gentle bob so the pickup reads as something to grab.
            if (!collected) transform.localPosition = restPosition + new Vector3(0f, 0.06f * Mathf.Sin(Time.time * 3f + bobPhase), 0f);
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Once only: the player's colliders can enter in the same step before the pickup is gone.
            if (collected) return;
            if (other.CompareTag("Player"))
            {
                Collect(other.gameObject);
            }
        }
        
        private void Collect(GameObject player)
        {
            collected = true;
            GetComponent<Collider2D>().enabled = false;
            if (type == CollectibleType.Health)
            {
                var health = player.GetComponent<Player.PlayerHealth>();
                if (health != null) health.Heal(value);
            }
            
            OnCollected?.Invoke(type, value); // the AudioManager plays the collect sound
            StartCoroutine(Pop());
        }
        
        /// <summary>A quick swell and fade, then the pickup is gone.</summary>
        private System.Collections.IEnumerator Pop()
        {
            var sr = GetComponent<SpriteRenderer>();
            Vector3 scale = transform.localScale;
            Color color = sr != null ? sr.color : Color.white;
            for (float t = 0f; t < PopTime; t += Time.deltaTime)
            {
                float k = t / PopTime;
                transform.localScale = scale * (1f + 0.6f * k);
                if (sr != null) sr.color = new Color(color.r, color.g, color.b, color.a * (1f - k));
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
