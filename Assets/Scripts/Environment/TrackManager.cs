using System.Collections.Generic;
using UnityEngine;
using DinosaurRunner.Obstacles;
using DinosaurRunner.Coins;
using DinosaurRunner.Dinosaurs;

namespace DinosaurRunner.Environment
{
    /// <summary>
    /// Core endless track system: recycles modular track segments continuously ahead
    /// of the player, managing obstacle, coin, and dinosaur spawns.
    /// </summary>
    public class TrackManager : MonoBehaviour
    {
        [Header("Track Configuration")]
        [SerializeField] private GameObject trackSegmentPrefab;
        [SerializeField] private float segmentLength = 30f;
        [SerializeField] private int initialSegments = 6;
        [SerializeField] private float safeDistance = 45f; // Distance before first obstacles appear

        [Header("System References")]
        [SerializeField] private EnvironmentSpawner envSpawner;
        [SerializeField] private ObstacleSpawner obstacleSpawner;
        [SerializeField] private CoinSpawner coinSpawner;
        [SerializeField] private DinosaurSpawner dinosaurSpawner;

        private Transform playerTransform;
        private readonly Queue<GameObject> activeSegments = new Queue<GameObject>();
        private float currentSpawnZ = 0f;
        private float startPlayerZ = 0f;

        private void Start()
        {
            FindPlayer();
            InitializeTrack();
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                var player = FindAnyObjectByType<Player.PlayerController>();
                if (player != null)
                {
                    playerTransform = player.transform;
                    startPlayerZ = playerTransform.position.z;
                }
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }

            // If player has moved forward enough, spawn new segment ahead and recycle old behind
            float playerZ = playerTransform.position.z;
            float lookAheadDist = initialSegments * segmentLength;

            while (currentSpawnZ < playerZ + lookAheadDist)
            {
                SpawnNextSegment();
            }

            // Recycle segments that are far behind player
            float cutoffZ = playerZ - segmentLength * 1.5f;

            if (activeSegments.Count > 0 && activeSegments.Peek().transform.position.z + segmentLength < cutoffZ)
            {
                GameObject oldSeg = activeSegments.Dequeue();
                Destroy(oldSeg);

                // Recycle obstacles, coins, and dinosaurs behind cutoff
                obstacleSpawner?.RecycleBehind(cutoffZ);
                coinSpawner?.RecycleBehind(cutoffZ);
                dinosaurSpawner?.RecycleBehind(cutoffZ);
            }
        }

        public void InitializeTrack()
        {
            ClearTrack();
            currentSpawnZ = startPlayerZ - segmentLength; // Start slightly behind player

            for (int i = 0; i < initialSegments; i++)
            {
                SpawnNextSegment(isInitial: true);
            }
        }

        private void SpawnNextSegment(bool isInitial = false)
        {
            GameObject segment;

            if (trackSegmentPrefab != null)
            {
                segment = Instantiate(trackSegmentPrefab, new Vector3(0, 0, currentSpawnZ), Quaternion.identity, transform);
            }
            else
            {
                segment = CreateDefaultTrackSegment(currentSpawnZ);
            }

            segment.name = $"TrackSegment_{Mathf.RoundToInt(currentSpawnZ)}";
            activeSegments.Enqueue(segment);

            // Populate environment flora & rocks
            envSpawner?.PopulateChunk(segment, currentSpawnZ, segmentLength);

            // Populate dinosaurs on track borders
            if (dinosaurSpawner != null && Random.value < 0.65f)
            {
                int side = Random.value > 0.5f ? 1 : -1;
                dinosaurSpawner.SpawnDinosaurAlongTrack(currentSpawnZ + segmentLength * 0.5f, side);
            }

            // Spawn obstacles & coins only past safe initial distance
            if (!isInitial || currentSpawnZ > startPlayerZ + safeDistance)
            {
                float difficultyRatio = Mathf.Clamp01((currentSpawnZ - startPlayerZ) / 800f);

                // Spawn obstacles
                if (obstacleSpawner != null && Random.value < 0.75f)
                {
                    float obstZ = currentSpawnZ + Random.Range(6f, segmentLength - 6f);
                    obstacleSpawner.SpawnPattern(obstZ, difficultyRatio);
                }

                // Spawn coins
                if (coinSpawner != null && Random.value < 0.85f)
                {
                    float coinZ = currentSpawnZ + Random.Range(2f, segmentLength - 12f);
                    coinSpawner.SpawnPattern(coinZ);
                }
            }

            currentSpawnZ += segmentLength;
        }

        private GameObject CreateDefaultTrackSegment(float zPos)
        {
            GameObject root = new GameObject("ProceduralTrackSegment");
            root.transform.SetParent(transform);
            root.transform.position = new Vector3(0, 0, zPos);

            // Running Road surface
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "RoadSurface";
            road.transform.SetParent(root.transform);
            road.transform.localPosition = new Vector3(0, -0.05f, segmentLength * 0.5f);
            road.transform.localScale = new Vector3(7.5f, 0.1f, segmentLength);

            // Ground Jungle terrain
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "SideGround";
            ground.transform.SetParent(root.transform);
            ground.transform.localPosition = new Vector3(0, -0.15f, segmentLength * 0.5f);
            ground.transform.localScale = new Vector3(50f, 0.1f, segmentLength);

            return root;
        }

        public void ClearTrack()
        {
            while (activeSegments.Count > 0)
            {
                GameObject seg = activeSegments.Dequeue();
                if (seg != null) Destroy(seg);
            }

            obstacleSpawner?.RecycleAll();
            coinSpawner?.RecycleAll();
            dinosaurSpawner?.RecycleAll();
        }

        public void ConfigureComponents(EnvironmentSpawner env, ObstacleSpawner obst, CoinSpawner coin, DinosaurSpawner dino, GameObject segPrefab = null)
        {
            envSpawner = env;
            obstacleSpawner = obst;
            coinSpawner = coin;
            dinosaurSpawner = dino;
            if (segPrefab != null) trackSegmentPrefab = segPrefab;
        }
    }
}

