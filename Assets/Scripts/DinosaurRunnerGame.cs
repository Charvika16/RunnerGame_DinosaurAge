using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// COMPLETE 3D DINOSAUR RUNNER GAME (Clean, Polished, Self-Contained)
/// - Clean 3-lane endless runner (Left -2.5, Center 0, Right +2.5)
/// - Human 3D runner character with Run, Jump (Space), Slide (S/Down)
/// - Neat tree-lined road with round green canopies & brown trunks
/// - Ground-level gold coins in neat rows (collected with chimes)
/// - Single-lane obstacles (smooth grey rocks, low jump logs, high slide branches)
/// - Real 3D animated dinosaurs along the grass that roar when approached
/// - Third-person follow camera, audio, HUD, Pause menu, Game Over, and saved High Score
/// </summary>
public class DinosaurRunnerGame : MonoBehaviour
{
    [Header("Player Settings")]
    public float initialSpeed = 10f;
    public float maxSpeed = 22f;
    public float speedIncreaseRate = 0.12f;
    public float laneDistance = 2.5f;
    public float laneSwitchSpeed = 16f;
    public float jumpForce = 9.5f;
    public float gravity = 28f;
    public float slideDuration = 0.75f;

    [Header("Road & Scenery")]
    public float roadWidth = 7.5f;
    public float treeRowX = 5.8f;
    public float treeSpacing = 7.0f;
    public float dinosaurSideX = 9.5f;

    [Header("Dinosaur Prefabs")]
    public GameObject[] dinosaurPrefabs;
    public float dinosaurScale = 2.8f;

    // Runtime state
    private GameObject playerObj;
    private CharacterController charController;
    private Animator playerAnimator;
    private Transform modelTransform;
    private Camera gameCamera;

    private int currentLane = 1; // 0=Left, 1=Center, 2=Right
    private float currentSpeed;
    private float targetX;
    private float verticalSpeed;
    private bool isSliding;
    private float slideTimer;
    private bool isGameOver;
    private bool isPaused;

    private int score;
    private int coins;
    private int bestScore;
    private float startZ;
    private float spawnZ;

    // Track, obstacle, coin & dinosaur pooling
    private readonly List<GameObject> activeRoadSegments = new List<GameObject>();
    private readonly List<GameObject> activeObstacles = new List<GameObject>();
    private readonly List<GameObject> activeCoins = new List<GameObject>();
    private readonly List<GameObject> activeDinosaurs = new List<GameObject>();

    // Shared materials
    private Material roadMat;
    private Material grassMat;
    private Material treeBarkMat;
    private Material leafMat;
    private Material rockMat;
    private Material logMat;
    private Material coinMat;

    // Audio
    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private AudioClip bgmClip;
    private AudioClip coinClip;
    private AudioClip jumpClip;
    private AudioClip slideClip;
    private AudioClip impactClip;
    private AudioClip gameOverClip;
    private readonly List<AudioClip> dinoRoarClips = new List<AudioClip>();

    // UI Styles
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle statStyle;
    private GUIStyle buttonStyle;
    private GUIStyle boxStyle;

    private void Awake()
    {
        currentSpeed = initialSpeed;
        bestScore = PlayerPrefs.GetInt("DinoRunner_BestScore", 0);
        Time.timeScale = 1f;

        CreateMaterials();
        SetupAudio();
    }

    private void Start()
    {
        Application.targetFrameRate = 60;

        CreatePlayer();
        SetupCamera();
        LoadDinosaurPrefabs();

        startZ = playerObj.transform.position.z;
        spawnZ = startZ + 15f;

        // Build initial 140m of road, trees, and scenery
        for (int i = 0; i < 7; i++)
        {
            CreateTrackSegment(i * 20f);
        }
    }

    private void Update()
    {
        if (isGameOver)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (isPaused)
            return;

        HandleInput();
        MovePlayer();
        FollowCamera();
        UpdateEndlessTrack();
        UpdateCoins();
        UpdateDinosaurs();
    }

    #region Materials & Setup

    private void CreateMaterials()
    {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) urpShader = Shader.Find("Standard");

        roadMat = CreateMat(urpShader, new Color(0.35f, 0.24f, 0.16f), 0.05f, 0.25f);
        grassMat = CreateMat(urpShader, new Color(0.18f, 0.45f, 0.18f), 0f, 0.35f);
        treeBarkMat = CreateMat(urpShader, new Color(0.38f, 0.22f, 0.10f), 0.05f, 0.2f);
        leafMat = CreateMat(urpShader, new Color(0.15f, 0.55f, 0.15f), 0f, 0.4f);
        rockMat = CreateMat(urpShader, new Color(0.62f, 0.65f, 0.68f), 0.05f, 0.3f);
        logMat = CreateMat(urpShader, new Color(0.42f, 0.24f, 0.12f), 0.05f, 0.25f);
        coinMat = CreateMat(urpShader, new Color(1.0f, 0.82f, 0.10f), 0.95f, 0.9f);
    }

    private Material CreateMat(Shader shader, Color color, float metallic, float smoothness)
    {
        Material m = new Material(shader);
        m.SetColor("_BaseColor", color);
        m.SetColor("_Color", color);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smoothness);
        return m;
    }

    private void SetupAudio()
    {
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = PlayerPrefs.GetFloat("MusicVolume", 0.65f);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = PlayerPrefs.GetFloat("SfxVolume", 0.85f);

#if UNITY_EDITOR
        bgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/prehistoric_theme.wav");
        coinClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/coin.ogg");
        jumpClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/jump.ogg");
        slideClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/slide.ogg");
        impactClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/impact.ogg");
        gameOverClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/gameover.ogg");

        AudioClip sRoar = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FerociousIndustries/PBRDinosaurs/PBRStegasaurus/Audio Clips/Roar1.wav");
        AudioClip rRoar = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FerociousIndustries/PBRDinosaurs/PBRVelociraptor/Audio Clips/RaptorRoar1.wav");
        AudioClip pRoar = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FerociousIndustries/PBRDinosaurs/PBRPachycephalasaurus/Audio Clips/Pachycephalosaurus_Roar.wav");

        if (sRoar != null) dinoRoarClips.Add(sRoar);
        if (rRoar != null) dinoRoarClips.Add(rRoar);
        if (pRoar != null) dinoRoarClips.Add(pRoar);
#endif

        if (bgmClip != null && !bgmSource.isPlaying)
        {
            bgmSource.clip = bgmClip;
            bgmSource.Play();
        }
    }

    private void LoadDinosaurPrefabs()
    {
        if (dinosaurPrefabs != null && dinosaurPrefabs.Length > 0)
            return;

#if UNITY_EDITOR
        GameObject stego = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FerociousIndustries/PBRDinosaurs/PBRStegasaurus/Prefabs/LODG/Stegasaurus_Green.prefab");
        GameObject raptor = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FerociousIndustries/PBRDinosaurs/PBRVelociraptor/Prefabs/Mobile/5K/Raptor_Animated_FBX_5K_Green.prefab");
        GameObject pachy = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FerociousIndustries/PBRDinosaurs/PBRPachycephalasaurus/Prefabs/PBR/Pachycephalasaurus_Green.prefab");

        var list = new List<GameObject>();
        if (stego != null) list.Add(stego);
        if (raptor != null) list.Add(raptor);
        if (pachy != null) list.Add(pachy);

        dinosaurPrefabs = list.ToArray();
#endif
    }

    #endregion

    #region Player Character

    private void CreatePlayer()
    {
        playerObj = new GameObject("PlayerCharacter");
        playerObj.transform.position = new Vector3(0, 0, 0);

        charController = playerObj.AddComponent<CharacterController>();
        charController.height = 1.8f;
        charController.radius = 0.35f;
        charController.center = new Vector3(0, 0.9f, 0);

        // Load 3D Human Model
        GameObject humanModelPrefab = null;
#if UNITY_EDITOR
        humanModelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Player/Male_Shirt.fbx");
        if (humanModelPrefab == null)
            humanModelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Player/Male_Casual.fbx");
#endif

        if (humanModelPrefab != null)
        {
            GameObject modelInst = Instantiate(humanModelPrefab, playerObj.transform);
            modelInst.name = "HumanMesh";
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
            modelInst.transform.localScale = Vector3.one;

            playerAnimator = modelInst.GetComponent<Animator>();
            modelTransform = modelInst.transform;
        }
        else
        {
            // Clean fallback visual
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "VisualModel";
            visual.transform.SetParent(playerObj.transform);
            visual.transform.localPosition = new Vector3(0, 0.9f, 0);
            visual.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            Destroy(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().sharedMaterial = CreateMat(Shader.Find("Universal Render Pipeline/Lit"), new Color(0.2f, 0.65f, 0.85f), 0.1f, 0.5f);
            modelTransform = visual.transform;
        }
    }

    private void SetupCamera()
    {
        gameCamera = Camera.main;
        if (gameCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            gameCamera = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();
        }

        gameCamera.fieldOfView = 60f;
        gameCamera.transform.position = new Vector3(0, 4.2f, -7.0f);
        gameCamera.transform.rotation = Quaternion.Euler(14f, 0, 0);
    }

    #endregion

    #region Input & Movement

    private void HandleInput()
    {
        // Lane left (A / Left Arrow)
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentLane > 0)
            {
                currentLane--;
                targetX = (currentLane - 1) * laneDistance;
            }
        }

        // Lane right (D / Right Arrow)
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentLane < 2)
            {
                currentLane++;
                targetX = (currentLane - 1) * laneDistance;
            }
        }

        // Jump (Space / Up Arrow)
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (charController.isGrounded && !isSliding)
            {
                verticalSpeed = jumpForce;
                PlaySFX(jumpClip, 0.85f);
            }
        }

        // Slide (S / Down Arrow)
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (!charController.isGrounded)
            {
                verticalSpeed = -jumpForce * 1.5f; // Fast-dive
            }

            if (!isSliding)
            {
                isSliding = true;
                slideTimer = slideDuration;

                charController.height = 0.7f;
                charController.center = new Vector3(0, 0.35f, 0);

                if (modelTransform != null)
                    modelTransform.localScale = new Vector3(1f, 0.55f, 1.25f);

                PlaySFX(slideClip, 0.8f);
            }
        }
    }

    private void MovePlayer()
    {
        // Speed ramps up gradually
        currentSpeed = Mathf.Min(currentSpeed + speedIncreaseRate * Time.deltaTime, maxSpeed);

        Vector3 pos = playerObj.transform.position;

        // Smooth horizontal lane transition
        float newX = Mathf.MoveTowards(pos.x, targetX, laneSwitchSpeed * Time.deltaTime);

        // Vertical physics
        if (charController.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -1.5f;
        else
            verticalSpeed -= gravity * Time.deltaTime;

        // Apply motion
        Vector3 move = new Vector3(newX - pos.x, verticalSpeed * Time.deltaTime, currentSpeed * Time.deltaTime);
        charController.Move(move);

        // Clamp to ground
        if (playerObj.transform.position.y < 0f)
        {
            Vector3 fixedPos = playerObj.transform.position;
            fixedPos.y = 0f;
            playerObj.transform.position = fixedPos;
            verticalSpeed = 0f;
        }

        // Slide timer
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f)
            {
                isSliding = false;
                charController.height = 1.8f;
                charController.center = new Vector3(0, 0.9f, 0);

                if (modelTransform != null)
                    modelTransform.localScale = Vector3.one;
            }
        }

        // Update distance score
        score = Mathf.Max(score, Mathf.FloorToInt(playerObj.transform.position.z - startZ));
    }

    private void FollowCamera()
    {
        Vector3 targetCamPos = new Vector3(
            Mathf.Lerp(gameCamera.transform.position.x, playerObj.transform.position.x * 0.6f, 6f * Time.deltaTime),
            playerObj.transform.position.y + 4.2f,
            playerObj.transform.position.z - 7.0f
        );

        gameCamera.transform.position = targetCamPos;
        gameCamera.transform.LookAt(playerObj.transform.position + new Vector3(0, 1.2f, 7.0f));
    }

    #endregion

    #region Track, Trees, Obstacles & Dinosaurs

    private void CreateTrackSegment(float z)
    {
        GameObject segment = new GameObject($"TrackSegment_{Mathf.RoundToInt(z)}");
        segment.transform.position = new Vector3(0, 0, z);

        // Center Dirt Road (width 7.5, length 20)
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Road";
        road.transform.SetParent(segment.transform);
        road.transform.localPosition = new Vector3(0, -0.05f, 10f);
        road.transform.localScale = new Vector3(roadWidth, 0.1f, 20f);
        road.GetComponent<Renderer>().sharedMaterial = roadMat;

        // Side Green Grass (wide, length 20)
        GameObject grass = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grass.name = "Grass";
        grass.transform.SetParent(segment.transform);
        grass.transform.localPosition = new Vector3(0, -0.15f, 10f);
        grass.transform.localScale = new Vector3(45f, 0.1f, 20f);
        grass.GetComponent<Renderer>().sharedMaterial = grassMat;

        // Trees along left and right side (just like the reference image!)
        for (float tz = 2f; tz < 20f; tz += treeSpacing)
        {
            CreateTree(-treeRowX, tz, segment.transform);
            CreateTree(treeRowX, tz, segment.transform);
        }

        // Prehistoric Dinosaurs beside track on the grass!
        if (dinosaurPrefabs != null && dinosaurPrefabs.Length > 0 && Random.value < 0.75f)
        {
            int side = (Random.value > 0.5f) ? 1 : -1;
            CreateDinosaur(side * dinosaurSideX, z + 10f);
        }

        activeRoadSegments.Add(segment);
    }

    private void CreateTree(float x, float z, Transform parent)
    {
        GameObject tree = new GameObject("Tree");
        tree.transform.SetParent(parent);
        tree.transform.localPosition = new Vector3(x, 0, z);

        // Brown cylindrical trunk
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform);
        trunk.transform.localPosition = new Vector3(0, 1.5f, 0);
        trunk.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
        Destroy(trunk.GetComponent<Collider>());
        trunk.GetComponent<Renderer>().sharedMaterial = treeBarkMat;

        // Round green canopy sphere
        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "Canopy";
        canopy.transform.SetParent(tree.transform);
        canopy.transform.localPosition = new Vector3(0, 3.5f, 0);
        canopy.transform.localScale = new Vector3(2.6f, 2.6f, 2.6f);
        Destroy(canopy.GetComponent<Collider>());
        canopy.GetComponent<Renderer>().sharedMaterial = leafMat;
    }

    private void CreateDinosaur(float x, float z)
    {
        GameObject prefab = dinosaurPrefabs[Random.Range(0, dinosaurPrefabs.Length)];
        if (prefab == null) return;

        float faceAngle = (x < 0) ? 90f : -90f; // Face toward the road
        GameObject dino = Instantiate(prefab, new Vector3(x, 0, z), Quaternion.Euler(0, faceAngle, 0));
        dino.name = "Dinosaur";
        dino.transform.localScale = Vector3.one * dinosaurScale;

        // Add 3D audio source for roars
        AudioSource aSrc = dino.AddComponent<AudioSource>();
        aSrc.spatialBlend = 1.0f;
        aSrc.minDistance = 4f;
        aSrc.maxDistance = 30f;
        aSrc.playOnAwake = false;

        activeDinosaurs.Add(dino);
    }

    private void UpdateEndlessTrack()
    {
        // Spawn ahead
        while (spawnZ < playerObj.transform.position.z + 100f)
        {
            CreateTrackSegment(spawnZ);
            SpawnObstaclesAndCoins(spawnZ);
            spawnZ += 20f;
        }

        // Recycle behind
        float cutoffZ = playerObj.transform.position.z - 25f;

        for (int i = activeRoadSegments.Count - 1; i >= 0; i--)
        {
            if (activeRoadSegments[i] == null || activeRoadSegments[i].transform.position.z + 20f < cutoffZ)
            {
                if (activeRoadSegments[i] != null) Destroy(activeRoadSegments[i]);
                activeRoadSegments.RemoveAt(i);
            }
        }

        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            if (activeObstacles[i] == null || activeObstacles[i].transform.position.z < cutoffZ)
            {
                if (activeObstacles[i] != null) Destroy(activeObstacles[i]);
                activeObstacles.RemoveAt(i);
            }
        }

        for (int i = activeCoins.Count - 1; i >= 0; i--)
        {
            if (activeCoins[i] == null || activeCoins[i].transform.position.z < cutoffZ)
            {
                if (activeCoins[i] != null) Destroy(activeCoins[i]);
                activeCoins.RemoveAt(i);
            }
        }

        for (int i = activeDinosaurs.Count - 1; i >= 0; i--)
        {
            if (activeDinosaurs[i] == null || activeDinosaurs[i].transform.position.z < cutoffZ)
            {
                if (activeDinosaurs[i] != null) Destroy(activeDinosaurs[i]);
                activeDinosaurs.RemoveAt(i);
            }
        }
    }

    private void SpawnObstaclesAndCoins(float z)
    {
        // 1. Spawn single-lane obstacle
        int obstLane = Random.Range(0, 3);
        float obstX = (obstLane - 1) * laneDistance;
        int obstType = Random.Range(0, 3); // 0=Rock, 1=Log, 2=Branch

        GameObject obstacle = null;

        if (obstType == 0)
        {
            // Smooth single-lane grey rock (fits cleanly in one lane!)
            obstacle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obstacle.name = "RockObstacle";
            obstacle.transform.position = new Vector3(obstX, 0.45f, z + 10f);
            obstacle.transform.localScale = new Vector3(1.3f, 0.9f, 1.3f);
            obstacle.GetComponent<Renderer>().sharedMaterial = rockMat;
        }
        else if (obstType == 1)
        {
            // Low wooden log (jumpable with Space!)
            obstacle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obstacle.name = "LowLogObstacle";
            obstacle.transform.position = new Vector3(obstX, 0.25f, z + 10f);
            obstacle.transform.rotation = Quaternion.Euler(0, 0, 90f);
            obstacle.transform.localScale = new Vector3(0.45f, 0.95f, 0.45f);
            obstacle.GetComponent<Renderer>().sharedMaterial = logMat;
        }
        else
        {
            // High branch arch (slideable under with S/Down!)
            obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "HighBranchObstacle";
            obstacle.transform.position = new Vector3(obstX, 1.85f, z + 10f);
            obstacle.transform.localScale = new Vector3(1.8f, 0.45f, 0.4f);
            obstacle.GetComponent<Renderer>().sharedMaterial = logMat;
        }

        // Set trigger collider for reliable hit detection
        Collider c = obstacle.GetComponent<Collider>();
        if (c != null) c.isTrigger = true;

        activeObstacles.Add(obstacle);

        // 2. Spawn neat row of 5 gold coins in a different lane at ground level!
        int coinLane = (obstLane + Random.Range(1, 3)) % 3;
        float coinX = (coinLane - 1) * laneDistance;

        for (int j = 0; j < 5; j++)
        {
            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "GoldCoin";
            // Ground level right in player's running path (y = 0.85m)
            coin.transform.position = new Vector3(coinX, 0.85f, z + 2f + (j * 1.8f));
            coin.transform.localScale = new Vector3(0.5f, 0.08f, 0.5f);
            coin.transform.rotation = Quaternion.Euler(90f, 0, 0);

            Collider cc = coin.GetComponent<Collider>();
            if (cc != null) cc.isTrigger = true;

            coin.GetComponent<Renderer>().sharedMaterial = coinMat;
            activeCoins.Add(coin);
        }
    }

    private void UpdateCoins()
    {
        for (int i = activeCoins.Count - 1; i >= 0; i--)
        {
            GameObject coin = activeCoins[i];
            if (coin == null)
            {
                activeCoins.RemoveAt(i);
                continue;
            }

            // Smooth coin spin
            coin.transform.Rotate(0, 0, 180f * Time.deltaTime, Space.Self);

            // Collect check
            float dist = Vector3.Distance(playerObj.transform.position + Vector3.up * 0.85f, coin.transform.position);
            if (dist < 1.35f)
            {
                coins++;
                PlaySFX(coinClip, 0.85f);
                Destroy(coin);
                activeCoins.RemoveAt(i);
            }
        }

        // Check obstacle collision
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            GameObject obst = activeObstacles[i];
            if (obst == null) continue;

            float dx = Mathf.Abs(playerObj.transform.position.x - obst.transform.position.x);
            float dz = Mathf.Abs(playerObj.transform.position.z - obst.transform.position.z);

            if (dx < 0.85f && dz < 0.85f)
            {
                bool isBranch = obst.name == "HighBranchObstacle";
                bool avoided;

                if (isBranch)
                {
                    // Avoided if sliding
                    avoided = isSliding;
                }
                else
                {
                    // Avoided if jumped high enough over the log/rock
                    avoided = !charController.isGrounded && playerObj.transform.position.y > 1.25f;
                }

                if (!avoided)
                {
                    GameOver();
                    return;
                }
            }
        }
    }

    private void UpdateDinosaurs()
    {
        // Proximity roars
        for (int i = 0; i < activeDinosaurs.Count; i++)
        {
            GameObject dino = activeDinosaurs[i];
            if (dino == null) continue;

            float dist = Vector3.Distance(playerObj.transform.position, dino.transform.position);
            if (dist < 16f)
            {
                AudioSource aSrc = dino.GetComponent<AudioSource>();
                if (aSrc != null && !aSrc.isPlaying && dinoRoarClips.Count > 0 && Random.value < 0.05f)
                {
                    AudioClip roar = dinoRoarClips[Random.Range(0, dinoRoarClips.Count)];
                    aSrc.PlayOneShot(roar, 0.9f);
                }
            }
        }
    }

    #endregion

    #region Game Flow, Audio & UI

    private void PlaySFX(AudioClip clip, float vol = 1f)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip, vol);
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void GameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f;

        PlaySFX(impactClip, 1f);
        PlaySFX(gameOverClip, 1f);

        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt("DinoRunner_BestScore", bestScore);
        }

        int totalCoins = PlayerPrefs.GetInt("DinoRunner_TotalCoins", 0) + coins;
        PlayerPrefs.SetInt("DinoRunner_TotalCoins", totalCoins);
        PlayerPrefs.Save();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void OnGUI()
    {
        InitStyles();

        // 1. In-Game HUD (Clean top-left badge)
        GUI.Box(new Rect(20, 20, 220, 85), GUIContent.none, boxStyle);
        GUI.Label(new Rect(35, 26, 190, 35), $"SCORE: {score}m", labelStyle);
        GUI.Label(new Rect(35, 60, 190, 35), $"COINS: {coins}", labelStyle);

        // Pause Button top right
        if (GUI.Button(new Rect(Screen.width - 120, 20, 100, 42), "PAUSE", buttonStyle))
        {
            TogglePause();
        }

        // 2. Pause Menu Modal
        if (isPaused)
        {
            float pw = 360f;
            float ph = 300f;
            float px = Screen.width / 2 - pw / 2;
            float py = Screen.height / 2 - ph / 2;

            GUI.Box(new Rect(px, py, pw, ph), GUIContent.none, boxStyle);
            GUI.Label(new Rect(px, py + 20, pw, 50), "GAME PAUSED", titleStyle);

            if (GUI.Button(new Rect(px + 60, py + 85, 240, 50), "RESUME", buttonStyle))
                TogglePause();
            if (GUI.Button(new Rect(px + 60, py + 145, 240, 50), "RESTART", buttonStyle))
                RestartGame();
            if (GUI.Button(new Rect(px + 60, py + 205, 240, 50), "MAIN MENU", buttonStyle))
                LoadMainMenu();
        }

        // 3. Game Over Screen Modal
        if (isGameOver)
        {
            float gw = 420f;
            float gh = 380f;
            float gx = Screen.width / 2 - gw / 2;
            float gy = Screen.height / 2 - gh / 2;

            GUI.Box(new Rect(gx, gy, gw, gh), GUIContent.none, boxStyle);
            GUI.Label(new Rect(gx, gy + 20, gw, 55), "GAME OVER", titleStyle);

            GUI.Label(new Rect(gx + 30, gy + 85, gw - 60, 35), $"FINAL DISTANCE: {score}m", statStyle);
            GUI.Label(new Rect(gx + 30, gy + 125, gw - 60, 35), $"COINS COLLECTED: {coins}", statStyle);
            GUI.Label(new Rect(gx + 30, gy + 165, gw - 60, 35), $"BEST RECORD: {bestScore}m", statStyle);

            if (GUI.Button(new Rect(gx + 80, gy + 225, 260, 52), "PLAY AGAIN", buttonStyle))
                RestartGame();
            if (GUI.Button(new Rect(gx + 80, gy + 290, 260, 52), "MAIN MENU", buttonStyle))
                LoadMainMenu();
        }
    }

    private void InitStyles()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(1f, 0.85f, 0.15f);

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = Color.white;

            statStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            statStyle.normal.textColor = Color.white;

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };

            boxStyle = new GUIStyle(GUI.skin.box);
        }
    }

    #endregion
}
