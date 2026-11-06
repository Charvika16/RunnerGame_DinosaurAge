using System.Collections.Generic;
using UnityEngine;

namespace DinosaurRunner.Dinosaurs
{
    /// <summary>
    /// Spawns massive animated dinosaurs naturally along the left and right sides
    /// of the running track with randomized poses, scales, and audio roars.
    /// </summary>
    public class DinosaurSpawner : MonoBehaviour
    {
        [Header("Dinosaur Prefabs")]
        [SerializeField] private GameObject[] dinosaurPrefabs;

        [Header("Placement Settings")]
        [SerializeField] private float sideDistanceMin = 7.5f;
        [SerializeField] private float sideDistanceMax = 12f;
        [SerializeField] private float minScale = 2.4f;
        [SerializeField] private float maxScale = 3.2f;

        [Header("Audio Clips by Dinosaur Type")]
        [SerializeField] private AudioClip[] stegosaurusClips;
        [SerializeField] private AudioClip[] raptorClips;
        [SerializeField] private AudioClip[] pachycephalosaurusClips;

        private readonly List<GameObject> activeDinosaurs = new List<GameObject>();
        private Transform dinosaursParent;

        private void Awake()
        {
            dinosaursParent = new GameObject("DinosaursContainer").transform;
            dinosaursParent.SetParent(transform);
        }

        public void SpawnDinosaurAlongTrack(float zPos, int side)
        {
            if (dinosaurPrefabs == null || dinosaurPrefabs.Length == 0)
                return;

            GameObject prefab = dinosaurPrefabs[Random.Range(0, dinosaurPrefabs.Length)];
            if (prefab == null) return;

            float xDist = Random.Range(sideDistanceMin, sideDistanceMax) * (side < 0 ? -1f : 1f);
            Vector3 spawnPos = new Vector3(xDist, 0f, zPos + Random.Range(-3f, 3f));

            // Face slightly angled towards the track
            float lookAngle = side < 0 ? Random.Range(70f, 110f) : Random.Range(-110f, -70f);
            Quaternion rotation = Quaternion.Euler(0f, lookAngle, 0f);

            GameObject dino = Instantiate(prefab, spawnPos, rotation, dinosaursParent);
            float scale = Random.Range(minScale, maxScale);
            dino.transform.localScale = Vector3.one * scale;

            DinosaurController ctrl = dino.GetComponent<DinosaurController>();
            if (ctrl == null)
                ctrl = dino.AddComponent<DinosaurController>();

            // Assign appropriate audio clips based on prefab name
            string lowerName = prefab.name.ToLower();
            if (lowerName.Contains("stega") && stegosaurusClips != null)
                ctrl.SetSpecies(DinosaurSpecies.Stegosaurus, stegosaurusClips);
            else if (lowerName.Contains("raptor") && raptorClips != null)
                ctrl.SetSpecies(DinosaurSpecies.Velociraptor, raptorClips);
            else if (pachycephalosaurusClips != null)
                ctrl.SetSpecies(DinosaurSpecies.Pachycephalosaurus, pachycephalosaurusClips);

            ctrl.ResetDinosaur();
            activeDinosaurs.Add(dino);
        }

        public void RecycleBehind(float cutoffZ)
        {
            for (int i = activeDinosaurs.Count - 1; i >= 0; i--)
            {
                GameObject dino = activeDinosaurs[i];
                if (dino == null || dino.transform.position.z < cutoffZ)
                {
                    if (dino != null)
                        Destroy(dino);
                    activeDinosaurs.RemoveAt(i);
                }
            }
        }

        public void RecycleAll()
        {
            for (int i = activeDinosaurs.Count - 1; i >= 0; i--)
            {
                if (activeDinosaurs[i] != null)
                    Destroy(activeDinosaurs[i]);
            }
            activeDinosaurs.Clear();
        }

        public void ConfigurePrefabs(GameObject[] prefabs, AudioClip[] stego, AudioClip[] raptor, AudioClip[] pachy)
        {
            dinosaurPrefabs = prefabs;
            stegosaurusClips = stego;
            raptorClips = raptor;
            pachycephalosaurusClips = pachy;
        }
    }
}
