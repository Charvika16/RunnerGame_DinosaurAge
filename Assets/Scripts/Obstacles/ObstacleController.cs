using UnityEngine;

namespace DinosaurRunner.Obstacles
{
    public enum ObstacleType
    {
        LowObstacle,    // Jump over (e.g., fallen log, rocks)
        HighObstacle,   // Slide under (e.g., high prehistoric branch, bone arch)
        LaneBlocker     // Change lane (e.g., giant boulder, dinosaur spike)
    }

    /// <summary>
    /// Controls obstacle behavior, collision classification, and destruction effects.
    /// </summary>
    public class ObstacleController : MonoBehaviour
    {
        [Header("Obstacle Configuration")]
        [SerializeField] private ObstacleType obstacleType = ObstacleType.LowObstacle;
        [SerializeField] private GameObject visualModel;
        [SerializeField] private ParticleSystem smashParticle;
        [SerializeField] private Collider obstacleCollider;

        public ObstacleType Type => obstacleType;
        public bool IsSmashed { get; private set; }

        private void Awake()
        {
            if (obstacleCollider == null)
                obstacleCollider = GetComponent<Collider>();
            if (visualModel == null && transform.childCount > 0)
                visualModel = transform.GetChild(0).gameObject;

            EnsureCollidersAreTriggers();
        }

        private void EnsureCollidersAreTriggers()
        {
            foreach (var c in GetComponentsInChildren<Collider>())
            {
                c.isTrigger = true;
            }
        }

        public void Setup(ObstacleType type)
        {
            obstacleType = type;
            ResetObstacle();
            EnsureCollidersAreTriggers();
        }

        public void Smash()
        {
            if (IsSmashed) return;
            IsSmashed = true;

            if (obstacleCollider != null)
                obstacleCollider.enabled = false;

            if (visualModel != null)
                visualModel.SetActive(false);

            if (smashParticle != null)
            {
                smashParticle.Play();
            }
        }

        public void ResetObstacle()
        {
            IsSmashed = false;
            if (obstacleCollider != null)
                obstacleCollider.enabled = true;
            if (visualModel != null)
                visualModel.SetActive(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsSmashed) return;

            Player.PlayerController player = other.GetComponent<Player.PlayerController>();
            if (player != null && !player.IsDead)
            {
                // If player is more than 1.1m away laterally, they are safely in another lane!
                if (Mathf.Abs(player.transform.position.x - transform.position.x) > 1.1f)
                    return;

                if (player.HasShield)
                {
                    Smash();
                }
                else
                {
                    // Check if player properly avoided the obstacle
                    bool avoided = false;

                    if (obstacleType == ObstacleType.LowObstacle)
                    {
                        // Avoided if player is jumping in the air
                        avoided = !player.IsGrounded || player.transform.position.y > 0.35f;
                    }
                    else if (obstacleType == ObstacleType.HighObstacle)
                    {
                        // Avoided if player is sliding underneath
                        avoided = player.IsSliding;
                    }

                    if (!avoided)
                    {
                        player.TakeDamage();
                    }
                }
            }
        }
    }
}
