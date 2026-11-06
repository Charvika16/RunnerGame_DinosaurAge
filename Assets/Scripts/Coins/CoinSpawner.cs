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

        public void SpawnPattern(float startZ, int patternType = -1)
        {
            if (patternType < 0)
                patternType = Random.Range(0, 4);

            int lane = Random.Range(0, 3);
            float xPos = (lane - 1) * laneDistance;

            switch (patternType)
            {
                case 0: // Straight Line of 5 coins
                    SpawnStraightLine(xPos, startZ, 5);
                    break;

                case 1: // Jump Arc (parabola for jumping)
                    SpawnJumpArc(xPos, startZ, 5);
                    break;

                case 2: // Diagonal across lanes
                    SpawnDiagonal(startZ);
                    break;

                case 3: // Coin cluster or Power-up
                    if (Random.value < powerupSpawnChance)
                    {
                        SpawnPowerup(xPos, startZ + 3f);
                    }
                    else
                    {
                        SpawnStraightLine(xPos, startZ, 4);
                    }
                    break;
            }
        }

        private void SpawnStraightLine(float x, float startZ, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Ground level coins right in the player's path
                Vector3 pos = new Vector3(x, 0.85f, startZ + i * 1.8f);
                SpawnCoin(pos, CollectibleType.Coin);
            }
        }

        private void SpawnJumpArc(float x, float startZ, int count)
        {
            float spacing = 1.8f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1); // 0 to 1
                // Gentle jump arc peaking at reachable 1.85m
                float arcHeight = 0.85f + 4.0f * t * (1.0f - t) * 1.0f;
                Vector3 pos = new Vector3(x, arcHeight, startZ + i * spacing);
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
