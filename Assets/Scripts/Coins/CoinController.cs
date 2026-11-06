using UnityEngine;
using DinosaurRunner.Audio;

namespace DinosaurRunner.Coins
{
    public enum CollectibleType
    {
        Coin,
        Magnet,
        Shield,
        Multiplier
    }

    /// <summary>
    /// Rotates collectible item, handles magnet pull toward player,
    /// and triggers collection visual/audio effects.
    /// </summary>
    public class CoinController : MonoBehaviour
    {
        [Header("Collectible Settings")]
        [SerializeField] private CollectibleType collectibleType = CollectibleType.Coin;
        [SerializeField] private int coinValue = 1;
        [SerializeField] private float rotationSpeed = 160f;
        [SerializeField] private float magnetSpeed = 22f;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem collectParticle;
        [SerializeField] private GameObject visualModel;

        public CollectibleType Type => collectibleType;
        public bool IsCollected { get; private set; }

        private Transform playerTransform;
        private Player.PlayerController playerController;

        private void Awake()
        {
            if (visualModel == null && transform.childCount > 0)
                visualModel = transform.GetChild(0).gameObject;
        }

        private void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            if (playerController == null)
            {
                playerController = FindAnyObjectByType<Player.PlayerController>();
                if (playerController != null)
                    playerTransform = playerController.transform;
            }
        }

        private void Update()
        {
            if (IsCollected)
                return;

            // Continuous rotation
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);

            // Handle Magnet Pull
            if (collectibleType == CollectibleType.Coin && playerController != null && playerController.HasMagnet)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= playerController.MagnetRadius)
                {
                    Vector3 targetPos = playerTransform.position + Vector3.up * 1f;
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, magnetSpeed * Time.deltaTime);

                    if (dist < 1.2f)
                    {
                        Collect();
                    }
                }
            }
        }

        public void Setup(CollectibleType type)
        {
            collectibleType = type;
            ResetCoin();
        }

        public void Collect()
        {
            if (IsCollected)
                return;

            IsCollected = true;

            // Trigger audio and scoring
            if (collectibleType == CollectibleType.Coin)
            {
                AudioManager.Instance?.PlayCoinSFX();
                UI.ScoreManager.Instance?.AddCoins(coinValue);
            }
            else if (collectibleType == CollectibleType.Magnet)
            {
                AudioManager.Instance?.PlayPowerupSFX();
                playerController?.ActivateMagnet(10f);
            }
            else if (collectibleType == CollectibleType.Shield)
            {
                AudioManager.Instance?.PlayPowerupSFX();
                playerController?.ActivateShield(8f);
            }
            else if (collectibleType == CollectibleType.Multiplier)
            {
                AudioManager.Instance?.PlayPowerupSFX();
                UI.ScoreManager.Instance?.ActivateMultiplier(2, 12f);
            }

            if (collectParticle != null)
            {
                collectParticle.transform.SetParent(null);
                collectParticle.Play();
                Destroy(collectParticle.gameObject, 1.5f);
            }

            gameObject.SetActive(false);
        }

        public void ResetCoin()
        {
            IsCollected = false;
            if (visualModel != null)
                visualModel.SetActive(true);
            FindPlayer();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsCollected)
                return;

            if (other.GetComponent<Player.PlayerController>() != null)
            {
                Collect();
            }
        }
    }
}

