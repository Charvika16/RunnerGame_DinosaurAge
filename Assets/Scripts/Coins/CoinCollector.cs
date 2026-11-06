using UnityEngine;

namespace DinosaurRunner.Coins
{
    /// <summary>
    /// Attached to the player to guarantee reliable high-speed coin collection
    /// and trigger power-up pickups.
    /// </summary>
    public class CoinCollector : MonoBehaviour
    {
        [Header("Collection Settings")]
        [SerializeField] private float collectionRadius = 1.35f;
        [SerializeField] private LayerMask collectibleLayer = ~0;

        private readonly Collider[] hitColliders = new Collider[10];

        private void Update()
        {
            CheckNearbyCollectibles();
        }

        private void CheckNearbyCollectibles()
        {
            int hits = Physics.OverlapSphereNonAlloc(
                transform.position + Vector3.up * 0.9f,
                collectionRadius,
                hitColliders,
                collectibleLayer,
                QueryTriggerInteraction.Collide
            );

            for (int i = 0; i < hits; i++)
            {
                CoinController coin = hitColliders[i].GetComponent<CoinController>();
                if (coin != null && !coin.IsCollected)
                {
                    coin.Collect();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            CoinController coin = other.GetComponent<CoinController>();
            if (coin != null && !coin.IsCollected)
            {
                coin.Collect();
            }
        }
    }
}
