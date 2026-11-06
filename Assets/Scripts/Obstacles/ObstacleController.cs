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
        }

        public void Setup(ObstacleType type)
        {
            obstacleType = type;
            ResetObstacle();
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
                        // Avoided if player is jumping high enough
                        avoided = !player.IsGrounded && player.transform.position.y > 1.25f;
                    }
                    else if (obstacleType == ObstacleType.HighObstacle)
                    {
                        // Avoided if player is sliding underneath
                        avoided = player.IsSliding;
                    }

                    if (!avoided)
                    {
                        player.Die();
                    }
                }
            }
        }
    }
}
