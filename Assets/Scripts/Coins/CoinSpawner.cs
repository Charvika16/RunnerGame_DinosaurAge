using System.Collections.Generic;
using UnityEngine;

namespace DinosaurRunner.Coins
{
    /// <summary>
    /// Spawns coin patterns (straight lines, jump arcs, lane diagonals)
    /// and rare power-up items using pooled objects.
    /// </summary>
    public class CoinSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private GameObject magnetPrefab;
        [SerializeField] private GameObject shieldPrefab;

        [Header("Settings")]
        [SerializeField] private float laneDistance = 2.5f;
        [SerializeField] private int initialPoolSize = 40;
        [SerializeField] private float powerupSpawnChance = 0.12f;

        private readonly List<CoinController> coinPool = new List<CoinController>();
        private readonly List<CoinController> activeCollectibles = new List<CoinController>();
        private Transform poolParent;

        private void Awake()
        {
            poolParent = new GameObject("CoinPool").transform;
            poolParent.SetParent(transform);
            InitializePool();
        }

        private void InitializePool()
        {
            if (coinPrefab != null)
            {
                for (int i = 0; i < initialPoolSize; i++)
                {
                    GameObject obj = Instantiate(coinPrefab, poolParent);
                    obj.SetActive(false);
                    CoinController ctrl = obj.GetComponent<CoinController>() ?? obj.AddComponent<CoinController>();
                    ctrl.Setup(CollectibleType.Coin);
                    coinPool.Add(ctrl);
                }
            }
        }

        public void SpawnPattern(float startZ, int preferredLane = -1, int patternType = -1)
        {
            if (patternType < 0)
                patternType = Random.Range(0, 3);

            int lane = (preferredLane >= 0 && preferredLane < 3) ? preferredLane : Random.Range(0, 3);
            float xPos = (lane - 1) * laneDistance;

            switch (patternType)
            {
                case 0: // Straight Line of 5 coins down the safe lane
                    SpawnStraightLine(xPos, startZ, 5);
                    break;

                case 1: // 4 coins down safe lane with clean spacing
                    SpawnStraightLine(xPos, startZ, 4);
                    break;

                case 2: // Power-up or 5 coins
                    if (Random.value < powerupSpawnChance)
                    {
                        SpawnPowerup(xPos, startZ + 2f);
                    }
                    else
                    {
                        SpawnStraightLine(xPos, startZ, 5);
                    }
                    break;
            }
        }

        private void SpawnStraightLine(float x, float startZ, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Ground level coins right in the player's path
                Vector3 pos = new Vector3(x, 0.85f, startZ + i * 2.0f);
                SpawnCoin(pos, CollectibleType.Coin);
            }
        }

        private void SpawnJumpArc(float x, float startZ, int count)
        {
            // Keep coins down on the path as requested by user
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = new Vector3(x, 0.85f, startZ + i * 2.0f);
                SpawnCoin(pos, CollectibleType.Coin);
            }
        }

        private void SpawnDiagonal(float startZ)
        {
            for (int lane = 0; lane < 3; lane++)
            {
                float x = (lane - 1) * laneDistance;
                Vector3 pos = new Vector3(x, 0.85f, startZ + lane * 2.2f);
                SpawnCoin(pos, CollectibleType.Coin);
            }
        }

        private void SpawnPowerup(float x, float z)
        {
            CollectibleType pType = Random.value > 0.5f ? CollectibleType.Magnet : CollectibleType.Shield;
            Vector3 pos = new Vector3(x, 0.95f, z);
            SpawnCoin(pos, pType);
        }

        private void SpawnCoin(Vector3 position, CollectibleType type)
        {
            CoinController coin = GetFromPool();
            if (coin != null)
            {
                coin.transform.position = position;
                coin.Setup(type);
                coin.gameObject.SetActive(true);
                activeCollectibles.Add(coin);
            }
        }

        private CoinController GetFromPool()
        {
            foreach (var item in coinPool)
            {
                if (!item.gameObject.activeInHierarchy)
                    return item;
            }

            if (coinPrefab != null)
            {
                GameObject newObj = Instantiate(coinPrefab, poolParent);
                CoinController ctrl = newObj.GetComponent<CoinController>() ?? newObj.AddComponent<CoinController>();
                ctrl.Setup(CollectibleType.Coin);
                coinPool.Add(ctrl);
                return ctrl;
            }

            return null;
        }

        public void RecycleBehind(float cutoffZ)
        {
            for (int i = activeCollectibles.Count - 1; i >= 0; i--)
            {
                CoinController coin = activeCollectibles[i];
                if (coin == null || coin.transform.position.z < cutoffZ)
                {
                    if (coin != null)
                        coin.gameObject.SetActive(false);
                    activeCollectibles.RemoveAt(i);
                }
            }
        }

        public void RecycleAll()
        {
            for (int i = activeCollectibles.Count - 1; i >= 0; i--)
            {
                if (activeCollectibles[i] != null)
                    activeCollectibles[i].gameObject.SetActive(false);
            }
            activeCollectibles.Clear();
        }

        public void SetPrefabs(GameObject coin, GameObject magnet, GameObject shield)
        {
            coinPrefab = coin;
            magnetPrefab = magnet;
            shieldPrefab = shield;
            InitializePool();
        }
    }
}
