using UnityEngine;

namespace MazeRunner.Levels
{
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        [SerializeField] private int damage = 1;
        [SerializeField] private bool instantKill = false;
        
        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                var health = other.GetComponent<Player.PlayerHealth>();
                if (health != null)
                {
                    if (instantKill)
                    {
                        health.Die();
                    }
                    else
                    {
                        health.TakeDamage(damage);
                    }
                }
            }
        }
    }
}
