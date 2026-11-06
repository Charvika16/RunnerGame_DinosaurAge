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

        private static Material sharedCoinMat;
        private static Material sharedMagnetMat;
        private static Material sharedShieldMat;

        private void Awake()
        {
            EnsureCleanVisualModel();
        }

        private void Start()
        {
            FindPlayer();
            EnsureCleanVisualModel();
        }

        private void EnsureCleanVisualModel()
        {
            // Deactivate any old broken FBX child model that had a 2m vertical offset
            Transform oldModel = transform.Find("CoinModel");
            if (oldModel != null)
            {
                oldModel.gameObject.SetActive(false);
            }

            Transform existingDisc = transform.Find("CoinDisc");
            if (existingDisc != null)
            {
                visualModel = existingDisc.gameObject;
                return;
            }

            if (visualModel == null || visualModel.name != "CoinDisc")
            {
                // Create a clean, centered, gold coin cylinder
                GameObject coinDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coinDisc.name = "CoinDisc";
                coinDisc.transform.SetParent(transform);
                coinDisc.transform.localPosition = Vector3.zero;
                // Rotate 90 around X so flat face faces along the road
                coinDisc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                coinDisc.transform.localScale = new Vector3(0.7f, 0.07f, 0.7f);

                Collider discCol = coinDisc.GetComponent<Collider>();
                if (discCol != null)
                {
                    if (Application.isPlaying) Destroy(discCol);
                    else DestroyImmediate(discCol);
                }

                if (sharedCoinMat == null)
                {
                    Shader s = Shader.Find("Universal Render Pipeline/Lit");
                    if (s == null) s = Shader.Find("Standard");

                    sharedCoinMat = new Material(s);
                    sharedCoinMat.SetColor("_BaseColor", new Color(1.0f, 0.82f, 0.05f));
                    sharedCoinMat.SetFloat("_Metallic", 0.95f);
                    sharedCoinMat.SetFloat("_Smoothness", 0.9f);

                    sharedMagnetMat = new Material(s);
                    sharedMagnetMat.SetColor("_BaseColor", new Color(1.0f, 0.35f, 0.05f));
                    sharedMagnetMat.SetFloat("_Metallic", 0.7f);
                    sharedMagnetMat.SetFloat("_Smoothness", 0.8f);

                    sharedShieldMat = new Material(s);
                    sharedShieldMat.SetColor("_BaseColor", new Color(0.1f, 0.85f, 1.0f));
                    sharedShieldMat.SetFloat("_Metallic", 0.5f);
                    sharedShieldMat.SetFloat("_Smoothness", 0.85f);
                }

                coinDisc.GetComponent<Renderer>().sharedMaterial = GetMaterialForType(collectibleType);
                visualModel = coinDisc;
            }

            // Ensure root collider is a clean trigger
            SphereCollider sc = GetComponent<SphereCollider>();
            if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.65f;
            sc.center = Vector3.zero;
        }

        private Material GetMaterialForType(CollectibleType type)
        {
            if (type == CollectibleType.Magnet) return sharedMagnetMat ?? sharedCoinMat;
            if (type == CollectibleType.Shield) return sharedShieldMat ?? sharedCoinMat;
            return sharedCoinMat;
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

