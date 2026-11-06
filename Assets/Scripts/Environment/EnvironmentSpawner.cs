using System.Collections.Generic;
using UnityEngine;

namespace DinosaurRunner.Environment
{
    /// <summary>
    /// Spawns clean prehistoric tree-lined avenues, ancient bushes, ferns,
    /// and horizon scenery matching professional endless runner visuals.
    /// </summary>
    public class EnvironmentSpawner : MonoBehaviour
    {
        [Header("Materials")]
        [SerializeField] private Material treeBarkMaterial;
        [SerializeField] private Material leafMaterial;
        [SerializeField] private Material bushMaterial;
        [SerializeField] private Material stoneMaterial;

        [Header("Road Border Settings")]
        [SerializeField] private float roadTreeX = 5.6f;
        [SerializeField] private float treeSpacing = 6.0f;

        private void Awake()
        {
            LoadDefaultMaterials();
        }

        private void LoadDefaultMaterials()
        {
#if UNITY_EDITOR
            if (treeBarkMaterial == null)
                treeBarkMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TreeBark.mat");
            if (leafMaterial == null)
                leafMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LeafMaterial.mat");
            if (bushMaterial == null)
                bushMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BushMaterial.mat");
            if (stoneMaterial == null)
                stoneMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/StoneMaterial.mat");
#endif
        }

        /// <summary>
        /// Populates an endless track chunk with neat tree avenues, bushes, and rocks.
        /// </summary>
        public void PopulateChunk(GameObject chunk, float startZ, float chunkLength)
        {
            Transform chunkTransform = chunk.transform;
            LoadDefaultMaterials();

            // 1. Neat Tree Avenues lining the road on both sides (like in reference image)
            int treeCount = Mathf.FloorToInt(chunkLength / treeSpacing);
            for (int i = 0; i < treeCount; i++)
            {
                float z = startZ + i * treeSpacing + (treeSpacing * 0.5f);

                // Left row tree
                CreateTree(-roadTreeX, z, chunkTransform, 1.0f);

                // Right row tree
                CreateTree(roadTreeX, z, chunkTransform, 1.0f);
            }

            // 2. Dense Outer Jungle Trees (further away for lush forest depth)
            for (int i = 0; i < 4; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float x = (roadTreeX + Random.Range(3.5f, 10f)) * side;
                float z = startZ + Random.Range(2f, chunkLength - 2f);
                float scale = Random.Range(1.1f, 1.6f);
                CreateTree(x, z, chunkTransform, scale);
            }

            // 3. Prehistoric Bushes and Ferns along the grass
            for (int i = 0; i < 6; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float x = (roadTreeX + Random.Range(1.0f, 6.0f)) * side;
                float z = startZ + Random.Range(1f, chunkLength - 1f);
                CreateBush(x, z, chunkTransform);
            }

            // 4. Mossy prehistoric stones on outer grass
            for (int i = 0; i < 3; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float x = (roadTreeX + Random.Range(2.0f, 8.0f)) * side;
                float z = startZ + Random.Range(1f, chunkLength - 1f);
                CreateRock(x, z, chunkTransform);
            }
        }

        private void CreateTree(float x, float z, Transform parent, float scaleMultiplier)
        {
            GameObject tree = new GameObject("PrehistoricTree");
            tree.transform.SetParent(parent);
            tree.transform.position = new Vector3(x, 0f, z);

            // Trunk (smooth brown wood)
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = new Vector3(0, 1.6f * scaleMultiplier, 0);
            trunk.transform.localScale = new Vector3(0.55f * scaleMultiplier, 1.6f * scaleMultiplier, 0.55f * scaleMultiplier);
            Collider trunkCol = trunk.GetComponent<Collider>();
            if (trunkCol != null) Destroy(trunkCol);

            if (treeBarkMaterial != null)
                trunk.GetComponent<Renderer>().sharedMaterial = treeBarkMaterial;

            // Canopy (rich green leafy foliage)
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(tree.transform);
            canopy.transform.localPosition = new Vector3(0, 3.8f * scaleMultiplier, 0);
            canopy.transform.localScale = new Vector3(2.8f * scaleMultiplier, 2.8f * scaleMultiplier, 2.8f * scaleMultiplier);
            Collider canopyCol = canopy.GetComponent<Collider>();
            if (canopyCol != null) Destroy(canopyCol);

            if (leafMaterial != null)
                canopy.GetComponent<Renderer>().sharedMaterial = leafMaterial;
            else if (bushMaterial != null)
                canopy.GetComponent<Renderer>().sharedMaterial = bushMaterial;
        }

        private void CreateBush(float x, float z, Transform parent)
        {
            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.name = "PrehistoricBush";
            bush.transform.SetParent(parent);
            bush.transform.position = new Vector3(x, 0.45f, z);
            bush.transform.localScale = new Vector3(Random.Range(1.2f, 1.8f), Random.Range(0.8f, 1.2f), Random.Range(1.2f, 1.8f));
            Collider col = bush.GetComponent<Collider>();
            if (col != null) Destroy(col);

            if (bushMaterial != null)
                bush.GetComponent<Renderer>().sharedMaterial = bushMaterial;
        }

        private void CreateRock(float x, float z, Transform parent)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "LandscapeRock";
            rock.transform.SetParent(parent);
            rock.transform.position = new Vector3(x, 0.4f, z);
            rock.transform.localScale = new Vector3(Random.Range(1.0f, 1.6f), Random.Range(0.6f, 1.0f), Random.Range(1.0f, 1.6f));
            Collider col = rock.GetComponent<Collider>();
            if (col != null) Destroy(col);

            if (stoneMaterial != null)
                rock.GetComponent<Renderer>().sharedMaterial = stoneMaterial;
        }

        public void ConfigureMaterials(Material bark, Material leaf, Material bush, Material stone)
        {
            treeBarkMaterial = bark;
            leafMaterial = leaf;
            bushMaterial = bush;
            stoneMaterial = stone;
        }

        public void ConfigureAssets(GameObject[] trees, GameObject[] bushes, GameObject[] rocks, GameObject mountain = null, GameObject volcano = null)
        {
            // Optional prefab override
        }
    }
}
