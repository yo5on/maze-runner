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
        
        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                Collect(other.gameObject);
            }
        }
        
        private void Collect(GameObject player)
        {
            if (type == CollectibleType.Health)
            {
                var health = player.GetComponent<Player.PlayerHealth>();
                if (health != null) health.Heal(value);
            }
            
            OnCollected?.Invoke(type, value);
            Destroy(gameObject);
        }
    }
}
