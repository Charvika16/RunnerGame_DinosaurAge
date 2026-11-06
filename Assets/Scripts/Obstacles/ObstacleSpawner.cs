using System.Collections.Generic;
using UnityEngine;

namespace DinosaurRunner.Obstacles
{
    /// <summary>
    /// Spawns procedural, guaranteed-fair obstacles along track chunks
    /// using object pooling to eliminate runtime allocations.
    /// </summary>
    public class ObstacleSpawner : MonoBehaviour
    {
        [Header("Obstacle Prefabs")]
        [SerializeField] private GameObject lowObstaclePrefab;   // Jumpable (log/rock)
        [SerializeField] private GameObject highObstaclePrefab;  // Slideable (branch/arch)
        [SerializeField] private GameObject blockerPrefab;       // Lane blocker (boulder)

        [Header("Spawn Settings")]
        [SerializeField] private float laneDistance = 2.5f;
        [SerializeField] private int initialPoolSize = 15;

        private readonly List<ObstacleController> lowPool = new List<ObstacleController>();
        private readonly List<ObstacleController> highPool = new List<ObstacleController>();
        private readonly List<ObstacleController> blockerPool = new List<ObstacleController>();
        private readonly List<ObstacleController> activeObstacles = new List<ObstacleController>();

        private Transform poolParent;

        private void Awake()
        {
            poolParent = new GameObject("ObstaclePool").transform;
            poolParent.SetParent(transform);
            InitializePools();
        }

        private void InitializePools()
        {
            if (lowObstaclePrefab != null)
                PopulatePool(lowObstaclePrefab, lowPool, ObstacleType.LowObstacle);
            if (highObstaclePrefab != null)
                PopulatePool(highObstaclePrefab, highPool, ObstacleType.HighObstacle);
            if (blockerPrefab != null)
                PopulatePool(blockerPrefab, blockerPool, ObstacleType.LaneBlocker);
        }

        private void PopulatePool(GameObject prefab, List<ObstacleController> pool, ObstacleType type)
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                GameObject obj = Instantiate(prefab, poolParent);
                obj.SetActive(false);
                ObstacleController ctrl = obj.GetComponent<ObstacleController>();
                if (ctrl == null)
                    ctrl = obj.AddComponent<ObstacleController>();

                ctrl.Setup(type);
                pool.Add(ctrl);
            }
        }

        /// <summary>
        /// Spawns a fair single-lane obstacle and returns a guaranteed safe free lane for coins.
        /// </summary>
        public int SpawnPattern(float zPosition, float difficultyRatio)
        {
            // Pick obstacle lane
            int obstLane = Random.Range(0, 3);
            int safeLane = (obstLane + Random.Range(1, 3)) % 3; // Guaranteed different lane!

            float xPos = (obstLane - 1) * laneDistance;
            ObstacleType type = (ObstacleType)Random.Range(0, 3);
            SpawnObstacle(type, new Vector3(xPos, 0f, zPosition));

            // Only at higher difficulty (> 400m), occasionally place a jumpable log in another lane
            if (difficultyRatio > 0.5f && Random.value < 0.4f)
            {
                int secondLane = 3 - obstLane - safeLane; // The third lane
                float xPos2 = (secondLane - 1) * laneDistance;
                SpawnObstacle(ObstacleType.LowObstacle, new Vector3(xPos2, 0f, zPosition));
            }

            return safeLane;
        }

        private void SpawnObstacle(ObstacleType type, Vector3 position)
        {
            ObstacleController obstacle = GetFromPool(type);
            if (obstacle != null)
            {
                obstacle.transform.position = position;
                obstacle.transform.rotation = Quaternion.identity;
                obstacle.ResetObstacle();
                obstacle.gameObject.SetActive(true);
                activeObstacles.Add(obstacle);
            }
        }

        private ObstacleController GetFromPool(ObstacleType type)
        {
            List<ObstacleController> pool = type switch
            {
                ObstacleType.LowObstacle => lowPool,
                ObstacleType.HighObstacle => highPool,
                _ => blockerPool
            };

            foreach (var item in pool)
            {
                if (!item.gameObject.activeInHierarchy)
                    return item;
            }

            // Expand pool if necessary
            GameObject prefab = type switch
            {
                ObstacleType.LowObstacle => lowObstaclePrefab,
                ObstacleType.HighObstacle => highObstaclePrefab,
                _ => blockerPrefab
            };

            if (prefab != null)
            {
                GameObject newObj = Instantiate(prefab, poolParent);
                ObstacleController newCtrl = newObj.GetComponent<ObstacleController>() ?? newObj.AddComponent<ObstacleController>();
                newCtrl.Setup(type);
                pool.Add(newCtrl);
                return newCtrl;
            }

            return null;
        }

        /// <summary>
        /// Recycles all obstacles that have fallen behind the cutoff Z position.
        /// </summary>
        public void RecycleBehind(float cutoffZ)
        {
            for (int i = activeObstacles.Count - 1; i >= 0; i--)
            {
                ObstacleController obstacle = activeObstacles[i];
                if (obstacle == null || obstacle.transform.position.z < cutoffZ)
                {
                    if (obstacle != null)
                    {
                        obstacle.gameObject.SetActive(false);
                    }
                    activeObstacles.RemoveAt(i);
                }
            }
        }

        public void RecycleAll()
        {
            for (int i = activeObstacles.Count - 1; i >= 0; i--)
            {
                if (activeObstacles[i] != null)
                    activeObstacles[i].gameObject.SetActive(false);
            }
            activeObstacles.Clear();
        }

        public void SetPrefabs(GameObject low, GameObject high, GameObject blocker)
        {
            lowObstaclePrefab = low;
            highObstaclePrefab = high;
            blockerPrefab = blocker;
            InitializePools();
        }
    }
}
