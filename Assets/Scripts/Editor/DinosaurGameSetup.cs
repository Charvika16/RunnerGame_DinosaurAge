using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using DinosaurRunner.Player;
using DinosaurRunner.Dinosaurs;
using DinosaurRunner.Environment;
using DinosaurRunner.Obstacles;
using DinosaurRunner.Coins;
using DinosaurRunner.CameraSystem;
using DinosaurRunner.UI;
using DinosaurRunner.Audio;

namespace DinosaurRunner.Editor
{
    /// <summary>
    /// Automated setup script to configure all materials, models, animators,
    /// prefabs, scenes, and build settings for the complete 3D Dinosaur Runner game.
    /// </summary>
    [InitializeOnLoad]
    public static class DinosaurGameSetup
    {
        private const string SetupDoneKey = "DinoRunner_SetupVersion_1_0";

        static DinosaurGameSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(SetupDoneKey, false))
                {
                    SessionState.SetBool(SetupDoneKey, true);
                    SetupCompleteGame();
                }
            };
        }

        [MenuItem("Dinosaur Game/Setup Complete Game")]
        public static void SetupCompleteGame()
        {
            Debug.Log("===> STARTING COMPLETE DINOSAUR RUNNER GAME SETUP <===");

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            EnsureDirectories();
            CreateMaterials();
            SetupPlayerModelAndAnimator();
            CreatePrefabs();
            SetupGameScene();
            SetupMainMenuScene();
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("===> DINOSAUR RUNNER SETUP COMPLETED SUCCESSFULLY! <===");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = new[]
            {
                "Assets/Materials",
                "Assets/Animations",
                "Assets/Prefabs/Player",
                "Assets/Prefabs/Obstacles",
                "Assets/Prefabs/Coins",
                "Assets/Prefabs/Environment",
                "Assets/Prefabs/Tracks",
                "Assets/Scenes"
            };

            foreach (var d in dirs)
            {
                if (!AssetDatabase.IsValidFolder(d))
                {
                    string parent = Path.GetDirectoryName(d).Replace("\\", "/");
                    string folder = Path.GetFileName(d);
                    AssetDatabase.CreateFolder(parent, folder);
                }
            }
        }

        private static Material GetOrCreateMaterial(string path, Color baseColor, float metallic = 0f, float smoothness = 0.5f, Color? emissionColor = null)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader == null) urpShader = Shader.Find("Standard");

                mat = new Material(urpShader);
                mat.SetColor("_BaseColor", baseColor);
                mat.SetColor("_Color", baseColor);
                mat.SetFloat("_Metallic", metallic);
                mat.SetFloat("_Smoothness", smoothness);

                if (emissionColor.HasValue && emissionColor.Value != Color.black)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emissionColor.Value);
                }

                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void CreateMaterials()
        {
            // Gold Coin Material
            GetOrCreateMaterial("Assets/Materials/CoinGold.mat", new Color(1f, 0.82f, 0.12f), 0.9f, 0.85f, new Color(0.35f, 0.25f, 0.05f));

            // Road Earth Material
            GetOrCreateMaterial("Assets/Materials/PrehistoricRoad.mat", new Color(0.32f, 0.22f, 0.14f), 0.05f, 0.25f);

            // Prehistoric Jungle Ground Material
            GetOrCreateMaterial("Assets/Materials/PrehistoricGrass.mat", new Color(0.18f, 0.38f, 0.12f), 0.0f, 0.35f);

            // Volcanic Rock / Boulder Material
            GetOrCreateMaterial("Assets/Materials/VolcanicBasalt.mat", new Color(0.18f, 0.16f, 0.16f), 0.1f, 0.3f);

            // Ancient Log / Wood Material
            GetOrCreateMaterial("Assets/Materials/AncientLog.mat", new Color(0.38f, 0.22f, 0.10f), 0.05f, 0.2f);

            // Lava Glow Material
            GetOrCreateMaterial("Assets/Materials/LavaGlow.mat", new Color(1f, 0.28f, 0.02f), 0.0f, 0.9f, new Color(1.8f, 0.45f, 0.05f));

            // Shield Bubble Material (Semi-transparent cyan)
            GetOrCreateMaterial("Assets/Materials/ShieldBubble.mat", new Color(0.2f, 0.75f, 1f, 0.45f), 0.2f, 0.85f, new Color(0.1f, 0.5f, 1f));

            // Magnet Aura Material (Semi-transparent gold)
            GetOrCreateMaterial("Assets/Materials/MagnetAura.mat", new Color(1f, 0.65f, 0.1f, 0.4f), 0.1f, 0.8f, new Color(0.8f, 0.4f, 0.05f));
        }

        private static void SetupPlayerModelAndAnimator()
        {
            string fbxPath = "Assets/Models/Player/Male_Shirt.fbx";
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.SaveAndReimport();
            }

            // Create Animator Controller
            string animControllerPath = "Assets/Animations/PlayerAnimator.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(animControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(animControllerPath);

                // Add Parameters
                controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
                controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
                controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
                controller.AddParameter("Slide", AnimatorControllerParameterType.Bool);
                controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
                controller.AddParameter("Reset", AnimatorControllerParameterType.Trigger);

                // Load Animation Clips from FBX
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                AnimationClip runClip = null;
                AnimationClip idleClip = null;
                AnimationClip jumpClip = null;

                foreach (var a in assets)
                {
                    if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        string nameLower = clip.name.ToLower();
                        if (nameLower.Contains("run")) runClip = clip;
                        else if (nameLower.Contains("idle")) idleClip = clip;
                        else if (nameLower.Contains("jump")) jumpClip = clip;
                    }
                }

                // If specific clips not found, use first available clip
                if (runClip == null)
                {
                    foreach (var a in assets)
                    {
                        if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                        {
                            runClip = clip;
                            break;
                        }
                    }
                }

                var rootStateMachine = controller.layers[0].stateMachine;

                // Run State (Default)
                var runState = rootStateMachine.AddState("Run");
                if (runClip != null) runState.motion = runClip;
                runState.speed = 1.15f;
                rootStateMachine.defaultState = runState;

                // Jump State
                var jumpState = rootStateMachine.AddState("Jump");
                if (jumpClip != null) jumpState.motion = jumpClip;
                else jumpState.motion = runClip;

                // Slide State
                var slideState = rootStateMachine.AddState("Slide");
                slideState.motion = runClip;
                slideState.speed = 1.3f;

                // Death State
                var dieState = rootStateMachine.AddState("Die");

                // Transitions
                var toJump = rootStateMachine.AddAnyStateTransition(jumpState);
                toJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
                toJump.hasExitTime = false;
                toJump.duration = 0.08f;

                var fromJump = jumpState.AddTransition(runState);
                fromJump.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
                fromJump.hasExitTime = false;
                fromJump.duration = 0.15f;

                var toSlide = rootStateMachine.AddAnyStateTransition(slideState);
                toSlide.AddCondition(AnimatorConditionMode.If, 0, "Slide");
                toSlide.hasExitTime = false;
                toSlide.duration = 0.08f;

                var fromSlide = slideState.AddTransition(runState);
                fromSlide.AddCondition(AnimatorConditionMode.IfNot, 0, "Slide");
                fromSlide.hasExitTime = false;
                fromSlide.duration = 0.15f;

                var toDie = rootStateMachine.AddAnyStateTransition(dieState);
                toDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
                toDie.hasExitTime = false;
                toDie.duration = 0.1f;

                var fromDie = dieState.AddTransition(runState);
                fromDie.AddCondition(AnimatorConditionMode.If, 0, "Reset");
                fromDie.hasExitTime = false;
                fromDie.duration = 0.1f;
            }
        }

        private static void CreatePrefabs()
        {
            CreatePlayerPrefab();
            CreateCoinPrefab();
            CreateObstaclePrefabs();
            CreateTrackSegmentPrefab();
        }

        private static void CreatePlayerPrefab()
        {
            string playerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
            GameObject root = new GameObject("Player");

            // Character Controller
            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);

            // Instantiate Visual Character Model
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Player/Male_Shirt.fbx");
            GameObject modelInstance;
            if (modelPrefab != null)
            {
                modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, root.transform);
                modelInstance.name = "CharacterModel";
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one * 1.0f;
            }
            else
            {
                modelInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                modelInstance.name = "CharacterModel";
                modelInstance.transform.SetParent(root.transform);
                modelInstance.transform.localPosition = new Vector3(0, 0.9f, 0);
            }

            // Animator
            Animator animator = modelInstance.GetComponent<Animator>();
            if (animator == null) animator = modelInstance.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/PlayerAnimator.controller");

            // Shield Visual Bubble
            GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shield.name = "ShieldVisual";
            shield.transform.SetParent(root.transform);
            shield.transform.localPosition = new Vector3(0, 0.9f, 0);
            shield.transform.localScale = Vector3.one * 2.2f;
            Collider shieldCol = shield.GetComponent<Collider>();
            if (shieldCol != null) Object.DestroyImmediate(shieldCol);
            Material shieldMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ShieldBubble.mat");
            if (shieldMat != null) shield.GetComponent<Renderer>().sharedMaterial = shieldMat;
            shield.SetActive(false);

            // Magnet Visual Aura
            GameObject magnet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            magnet.name = "MagnetVisual";
            magnet.transform.SetParent(root.transform);
            magnet.transform.localPosition = new Vector3(0, 0.9f, 0);
            magnet.transform.localScale = Vector3.one * 2.4f;
            Collider magnetCol = magnet.GetComponent<Collider>();
            if (magnetCol != null) Object.DestroyImmediate(magnetCol);
            Material magnetMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MagnetAura.mat");
            if (magnetMat != null) magnet.GetComponent<Renderer>().sharedMaterial = magnetMat;
            magnet.SetActive(false);

            // Add Player Controllers
            PlayerAnimationController animCtrl = root.AddComponent<PlayerAnimationController>();
            PlayerController playerCtrl = root.AddComponent<PlayerController>();
            root.AddComponent<CoinCollector>();

            // Wire Serialized Properties
            SerializedObject so = new SerializedObject(playerCtrl);
            so.FindProperty("shieldVisual").objectReferenceValue = shield;
            so.FindProperty("magnetVisual").objectReferenceValue = magnet;
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(root, playerPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("Created Player Prefab at " + playerPrefabPath);
        }

        private static void CreateCoinPrefab()
        {
            string coinPrefabPath = "Assets/Prefabs/Coins/Coin.prefab";
            GameObject root = new GameObject("Coin");

            GameObject coinModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Props/coinGold.fbx");
            GameObject modelInst;
            if (coinModel != null)
            {
                modelInst = (GameObject)PrefabUtility.InstantiatePrefab(coinModel, root.transform);
                modelInst.name = "CoinModel";
                modelInst.transform.localPosition = Vector3.zero;
                modelInst.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                modelInst.transform.localScale = Vector3.one * 1.5f;
            }
            else
            {
                modelInst = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                modelInst.name = "CoinModel";
                modelInst.transform.SetParent(root.transform);
                modelInst.transform.localScale = new Vector3(0.6f, 0.08f, 0.6f);
                modelInst.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            }

            Material goldMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CoinGold.mat");
            if (goldMat != null)
            {
                foreach (var r in modelInst.GetComponentsInChildren<Renderer>())
                    r.sharedMaterial = goldMat;
            }

            SphereCollider col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.55f;

            root.AddComponent<CoinController>();

            PrefabUtility.SaveAsPrefabAsset(root, coinPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void CreateObstaclePrefabs()
        {
            Material logMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/AncientLog.mat");
            Material rockMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VolcanicBasalt.mat");

            // 1. Low Obstacle (Fallen Log - Jumpable)
            {
                GameObject root = new GameObject("LowObstacle_Log");
                GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                log.name = "LogModel";
                log.transform.SetParent(root.transform);
                log.transform.localPosition = new Vector3(0, 0.35f, 0);
                log.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                log.transform.localScale = new Vector3(0.6f, 1.25f, 0.6f);
                if (logMat != null) log.GetComponent<Renderer>().sharedMaterial = logMat;

                BoxCollider box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = new Vector3(0, 0.35f, 0);
                box.size = new Vector3(2.4f, 0.7f, 0.8f);

                ObstacleController ctrl = root.AddComponent<ObstacleController>();
                ctrl.Setup(ObstacleType.LowObstacle);

                PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Obstacles/LowObstacle_Log.prefab");
                Object.DestroyImmediate(root);
            }

            // 2. High Obstacle (Ancient Overhead Branch / Arch - Slideable)
            {
                GameObject root = new GameObject("HighObstacle_Branch");

                // Horizontal overhead beam
                GameObject branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                branch.name = "BranchModel";
                branch.transform.SetParent(root.transform);
                branch.transform.localPosition = new Vector3(0, 1.9f, 0);
                branch.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                branch.transform.localScale = new Vector3(0.45f, 1.35f, 0.45f);
                if (logMat != null) branch.GetComponent<Renderer>().sharedMaterial = logMat;

                // Support posts on edges
                GameObject postL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                postL.name = "PostLeft";
                postL.transform.SetParent(root.transform);
                postL.transform.localPosition = new Vector3(-1.3f, 1.0f, 0);
                postL.transform.localScale = new Vector3(0.3f, 1.0f, 0.3f);
                if (logMat != null) postL.GetComponent<Renderer>().sharedMaterial = logMat;

                GameObject postR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                postR.name = "PostRight";
                postR.transform.SetParent(root.transform);
                postR.transform.localPosition = new Vector3(1.3f, 1.0f, 0);
                postR.transform.localScale = new Vector3(0.3f, 1.0f, 0.3f);
                if (logMat != null) postR.GetComponent<Renderer>().sharedMaterial = logMat;

                // Slideable Trigger: Elevated from y=1.2 to 2.4, leaving 0-1.1m clear for sliding underneath!
                BoxCollider box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = new Vector3(0, 1.85f, 0);
                box.size = new Vector3(2.2f, 0.9f, 0.8f);

                ObstacleController ctrl = root.AddComponent<ObstacleController>();
                ctrl.Setup(ObstacleType.HighObstacle);

                PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Obstacles/HighObstacle_Branch.prefab");
                Object.DestroyImmediate(root);
            }

            // 3. Blocker Obstacle (Volcanic Boulder - Lane Blocker)
            {
                GameObject root = new GameObject("Blocker_Boulder");

                GameObject rockModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Props/rocks.fbx");
                GameObject modelInst;
                if (rockModel != null)
                {
                    modelInst = (GameObject)PrefabUtility.InstantiatePrefab(rockModel, root.transform);
                    modelInst.name = "RockModel";
                    modelInst.transform.localPosition = Vector3.zero;
                    modelInst.transform.localScale = Vector3.one * 1.8f;
                }
                else
                {
                    modelInst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    modelInst.name = "RockModel";
                    modelInst.transform.SetParent(root.transform);
                    modelInst.transform.localPosition = new Vector3(0, 0.75f, 0);
                    modelInst.transform.localScale = new Vector3(1.5f, 1.3f, 1.3f);
                }

                if (rockMat != null)
                {
                    foreach (var r in modelInst.GetComponentsInChildren<Renderer>())
                        r.sharedMaterial = rockMat;
                }

                BoxCollider box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = new Vector3(0, 0.85f, 0);
                box.size = new Vector3(1.8f, 1.7f, 1.4f);

                ObstacleController ctrl = root.AddComponent<ObstacleController>();
                ctrl.Setup(ObstacleType.LaneBlocker);

                PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Obstacles/Blocker_Boulder.prefab");
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateTrackSegmentPrefab()
        {
            string trackPrefabPath = "Assets/Prefabs/Tracks/TrackSegment.prefab";
            GameObject root = new GameObject("TrackSegment");

            Material roadMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PrehistoricRoad.mat");
            Material grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PrehistoricGrass.mat");
            Material rockMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VolcanicBasalt.mat");

            // Road surface (width 7.5, length 30)
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "RoadSurface";
            road.transform.SetParent(root.transform);
            road.transform.localPosition = new Vector3(0, -0.05f, 15f);
            road.transform.localScale = new Vector3(7.5f, 0.1f, 30f);
            if (roadMat != null) road.GetComponent<Renderer>().sharedMaterial = roadMat;

            // Side stone curbs
            GameObject curbL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curbL.name = "CurbLeft";
            curbL.transform.SetParent(root.transform);
            curbL.transform.localPosition = new Vector3(-3.85f, 0.05f, 15f);
            curbL.transform.localScale = new Vector3(0.35f, 0.2f, 30f);
            if (rockMat != null) curbL.GetComponent<Renderer>().sharedMaterial = rockMat;

            GameObject curbR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curbR.name = "CurbRight";
            curbR.transform.SetParent(root.transform);
            curbR.transform.localPosition = new Vector3(3.85f, 0.05f, 15f);
            curbR.transform.localScale = new Vector3(0.35f, 0.2f, 30f);
            if (rockMat != null) curbR.GetComponent<Renderer>().sharedMaterial = rockMat;

            // Wide ground terrain (width 45, length 30)
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "SideGround";
            ground.transform.SetParent(root.transform);
            ground.transform.localPosition = new Vector3(0, -0.15f, 15f);
            ground.transform.localScale = new Vector3(48f, 0.1f, 30f);
            if (grassMat != null) ground.GetComponent<Renderer>().sharedMaterial = grassMat;

            PrefabUtility.SaveAsPrefabAsset(root, trackPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void SetupGameScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Directional Light (Warm prehistoric sunlight)
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.85f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            // 2. Camera
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.fieldOfView = 62f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 350f;
            cam.clearFlags = CameraClearFlags.Skybox;
            camObj.AddComponent<AudioListener>();
            CameraFollow camFollow = camObj.AddComponent<CameraFollow>();

            // 3. Audio Manager
            GameObject audioObj = new GameObject("AudioManager");
            AudioManager audioMgr = audioObj.AddComponent<AudioManager>();
            AudioClip bgm = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/prehistoric_theme.wav");
            AudioClip coinSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/coin.ogg");
            AudioClip jumpSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/jump.ogg");
            AudioClip slideSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/slide.ogg");
            AudioClip impactSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/impact.ogg");
            AudioClip gameOverSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/gameover.ogg");
            AudioClip powerupSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/powerup.ogg");
            AudioClip buttonSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/button.ogg");
            AudioClip footstepSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/footstep.wav");
            audioMgr.AssignClips(bgm, coinSfx, jumpSfx, slideSfx, impactSfx, gameOverSfx, powerupSfx, buttonSfx, footstepSfx);

            // 4. Player Instance
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            GameObject playerInst = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInst.transform.position = new Vector3(0, 0, 0);
            camFollow.SetTarget(playerInst.transform);

            // 5. Game Manager & Score Manager & UI Manager
            GameObject gameMgrObj = new GameObject("GameManager");
            gameMgrObj.AddComponent<GameManager.GameManager>();
            gameMgrObj.AddComponent<GameManager.GameOverManager>();
            gameMgrObj.AddComponent<ScoreManager>();
            gameMgrObj.AddComponent<UIManager>();

            // 6. Track & Spawner Systems
            GameObject trackSysObj = new GameObject("TrackSystem");
            TrackManager trackMgr = trackSysObj.AddComponent<TrackManager>();
            EnvironmentSpawner envSpawner = trackSysObj.AddComponent<EnvironmentSpawner>();
            ObstacleSpawner obstSpawner = trackSysObj.AddComponent<ObstacleSpawner>();
            CoinSpawner coinSpawner = trackSysObj.AddComponent<CoinSpawner>();
            DinosaurSpawner dinoSpawner = trackSysObj.AddComponent<DinosaurSpawner>();

            // Wire TrackManager references
            GameObject trackSegPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tracks/TrackSegment.prefab");
            trackMgr.ConfigureComponents(envSpawner, obstSpawner, coinSpawner, dinoSpawner, trackSegPrefab);

            // Configure Environment Foliage
            GameObject tree1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/BigTree.fbx");
            GameObject tree2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/Tree.fbx");
            GameObject tree3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/SmallTree.fbx");
            GameObject bush1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/BigBush.fbx");
            GameObject rock1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Props/rocks.fbx");
            envSpawner.ConfigureAssets(
                new[] { tree1, tree2, tree3 },
                new[] { bush1 },
                new[] { rock1 }
            );

            // Configure Obstacles
            GameObject lowObst = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Obstacles/LowObstacle_Log.prefab");
            GameObject highObst = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Obstacles/HighObstacle_Branch.prefab");
            GameObject blockerObst = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Obstacles/Blocker_Boulder.prefab");
            obstSpawner.SetPrefabs(lowObst, highObst, blockerObst);

            // Configure Coins
            GameObject coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Coins/Coin.prefab");
            coinSpawner.SetPrefabs(coinPrefab, coinPrefab, coinPrefab);

            // Configure Dinosaurs from FerociousIndustries
            GameObject stego = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FerociousIndustries/PBRDinosaurs/PBRStegasaurus/Prefabs/LODG/Stegasaurus_Green.prefab");
            if (stego == null) stego = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FerociousIndustries/PBRDinosaurs/PBRStegasaurus/Prefabs/20K/Stegasaurus_20K.prefab");

            GameObject raptor = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FerociousIndustries/PBRDinosaurs/PBRVelociraptor/Prefabs/Mobile/5K/Raptor_Animated_FBX_5K_Green.prefab");
            if (raptor == null) raptor = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FerociousIndustries/PBRDinosaurs/PBRVelociraptor/Prefabs/PBR/15K/PBR_Velociraptor_Green.prefab");

            GameObject pachy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FerociousIndustries/PBRDinosaurs/PBRPachycephalasaurus/Prefabs/PBR/Pachycephalasaurus_Green.prefab");
            if (pachy == null) pachy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FerociousIndustries/PBRDinosaurs/PBRPachycephalasaurus/Prefabs/24K/Pachycephalasaurus_24K.prefab");

            AudioClip stegoRoar = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FerociousIndustries/PBRDinosaurs/PBRStegasaurus/Audio Clips/Roar1.wav");
            AudioClip raptorRoar = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FerociousIndustries/PBRDinosaurs/PBRVelociraptor/Audio Clips/RaptorRoar1.wav");
            AudioClip pachyRoar = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FerociousIndustries/PBRDinosaurs/PBRPachycephalasaurus/Audio Clips/Pachycephalosaurus_Roar.wav");

            dinoSpawner.ConfigurePrefabs(
                new[] { stego, raptor, pachy },
                new[] { stegoRoar },
                new[] { raptorRoar },
                new[] { pachyRoar }
            );

            // Save Scene
            string scenePath = "Assets/Scenes/DinosaurLevel.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("Configured and saved DinosaurLevel scene at " + scenePath);
        }

        private static void SetupMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Lighting
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.92f, 0.82f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(35f, 40f, 0f);

            // 2. Scenic Camera
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.fieldOfView = 55f;
            cam.clearFlags = CameraClearFlags.Skybox;
            camObj.transform.position = new Vector3(0, 3.2f, -9f);
            camObj.transform.rotation = Quaternion.Euler(14f, 0f, 0f);
            camObj.AddComponent<AudioListener>();

            // 3. Audio Manager
            GameObject audioObj = new GameObject("AudioManager");
            AudioManager audioMgr = audioObj.AddComponent<AudioManager>();
            AudioClip bgm = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/prehistoric_theme.wav");
            AudioClip btnSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/button.ogg");
            audioMgr.AssignClips(bgm, null, null, null, null, null, null, btnSfx, null);

            // 4. Scenic Environment & Dinosaurs
            Material grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PrehistoricGrass.mat");
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "MenuGround";
            ground.transform.position = new Vector3(0, -0.2f, 5f);
            ground.transform.localScale = new Vector3(40f, 0.4f, 40f);
            if (grassMat != null) ground.GetComponent<Renderer>().sharedMaterial = grassMat;

            // Place scenic prehistoric trees
            GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/BigTree.fbx");
            if (treePrefab != null)
            {
                GameObject t1 = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab);
                t1.transform.position = new Vector3(-7f, 0, 8f);
                t1.transform.localScale = Vector3.one * 1.5f;

                GameObject t2 = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab);
                t2.transform.position = new Vector3(7.5f, 0, 10f);
                t2.transform.localScale = Vector3.one * 1.7f;
            }

            // Place scenic Stegosaurus and Velociraptor
            GameObject stegoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FerociousIndustries/PBRDinosaurs/PBRStegasaurus/Prefabs/LODG/Stegasaurus_Green.prefab");
            if (stegoPrefab != null)
            {
                GameObject stego = (GameObject)PrefabUtility.InstantiatePrefab(stegoPrefab);
                stego.transform.position = new Vector3(-3.5f, 0, 4f);
                stego.transform.rotation = Quaternion.Euler(0, 125f, 0);
                stego.transform.localScale = Vector3.one * 2.8f;
            }

            GameObject raptorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FerociousIndustries/PBRDinosaurs/PBRVelociraptor/Prefabs/Mobile/5K/Raptor_Animated_FBX_5K_Green.prefab");
            if (raptorPrefab != null)
            {
                GameObject raptor = (GameObject)PrefabUtility.InstantiatePrefab(raptorPrefab);
                raptor.transform.position = new Vector3(3.2f, 0, 2f);
                raptor.transform.rotation = Quaternion.Euler(0, -60f, 0);
                raptor.transform.localScale = Vector3.one * 1.8f;
            }

            // 5. Main Menu Manager
            GameObject menuMgrObj = new GameObject("MainMenuManager");
            menuMgrObj.AddComponent<MainMenuManager>();

            // Save Scene
            string scenePath = "Assets/Scenes/MainMenu.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("Configured and saved MainMenu scene at " + scenePath);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/DinosaurLevel.unity", true)
            };

            EditorBuildSettings.scenes = scenes;
            Debug.Log("Configured EditorBuildSettings: Scene 0 = MainMenu, Scene 1 = DinosaurLevel");
        }
    }
}
