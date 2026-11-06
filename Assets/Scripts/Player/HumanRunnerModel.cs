using UnityEngine;

namespace DinosaurRunner.Player
{
    /// <summary>
    /// Builds and animates a stylized 3D Human Runner with head, torso, limbs,
    /// and running/jumping/sliding procedural biomechanics.
    /// Guaranteed to render visibly in URP with vibrant materials.
    /// </summary>
    public class HumanRunnerModel : MonoBehaviour
    {
        [Header("Colors & Materials")]
        [SerializeField] private Color skinColor = new Color(0.92f, 0.74f, 0.58f);
        [SerializeField] private Color shirtColor = new Color(0.12f, 0.55f, 0.88f);  // Athletic Blue
        [SerializeField] private Color pantsColor = new Color(0.18f, 0.22f, 0.32f);  // Denim Dark
        [SerializeField] private Color bootsColor = new Color(0.28f, 0.16f, 0.08f);  // Leather Brown
        [SerializeField] private Color hairColor = new Color(0.25f, 0.15f, 0.08f);   // Dark Brown Hair
        [SerializeField] private Color capColor = new Color(0.85f, 0.22f, 0.15f);    // Explorer Red Cap

        // Bone / Limb Transforms for Animation
        private Transform torso;
        private Transform head;
        private Transform leftArmPivot;
        private Transform rightArmPivot;
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftLegPivot;
        private Transform rightLegPivot;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform leftCalf;
        private Transform rightCalf;

        private PlayerController playerController;
        private float runPhase;
        private float originalY;

        private void Awake()
        {
            playerController = GetComponentInParent<PlayerController>();
            BuildHumanBody();
        }

        private void Start()
        {
            if (torso == null)
            {
                BuildHumanBody();
            }
        }

        private void BuildHumanBody()
        {
            Transform existingTorso = transform.Find("Torso");
            if (existingTorso != null)
            {
                torso = existingTorso;
                head = torso.Find("Head");
                leftArmPivot = torso.Find("LeftArmPivot");
                rightArmPivot = torso.Find("RightArmPivot");
                leftLegPivot = transform.Find("LeftLegPivot");
                rightLegPivot = transform.Find("RightLegPivot");
                return; // Already built!
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material skinMat = MakeMat(shader, skinColor, 0f, 0.3f);
            Material shirtMat = MakeMat(shader, shirtColor, 0.05f, 0.4f);
            Material pantsMat = MakeMat(shader, pantsColor, 0f, 0.25f);
            Material bootsMat = MakeMat(shader, bootsColor, 0.1f, 0.3f);
            Material hairMat = MakeMat(shader, hairColor, 0f, 0.2f);
            Material capMat = MakeMat(shader, capColor, 0.05f, 0.35f);

            // Root Torso at waist height (y = 0.95m)
            GameObject torsoObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torsoObj.name = "Torso";
            torsoObj.transform.SetParent(transform);
            torsoObj.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            torsoObj.transform.localScale = new Vector3(0.52f, 0.62f, 0.32f);
            StripCollider(torsoObj);
            torsoObj.GetComponent<Renderer>().sharedMaterial = shirtMat;
            torso = torsoObj.transform;

            // Head (y = 0.48 above torso -> y ~ 1.43m)
            GameObject headObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headObj.name = "Head";
            headObj.transform.SetParent(torso);
            headObj.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            headObj.transform.localScale = new Vector3(0.68f, 0.58f, 0.72f);
            StripCollider(headObj);
            headObj.GetComponent<Renderer>().sharedMaterial = skinMat;
            head = headObj.transform;

            // Explorer Cap / Bandana on Head
            GameObject capObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            capObj.name = "Cap";
            capObj.transform.SetParent(head);
            capObj.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            capObj.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
            capObj.transform.localScale = new Vector3(0.95f, 0.18f, 0.95f);
            StripCollider(capObj);
            capObj.GetComponent<Renderer>().sharedMaterial = capMat;

            // Cap Visor / Bill facing forward (+Z)
            GameObject visorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visorObj.name = "Visor";
            visorObj.transform.SetParent(capObj.transform);
            visorObj.transform.localPosition = new Vector3(0f, -0.3f, 0.48f);
            visorObj.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            visorObj.transform.localScale = new Vector3(0.85f, 0.22f, 0.55f);
            StripCollider(visorObj);
            visorObj.GetComponent<Renderer>().sharedMaterial = capMat;

            // Hair back
            GameObject hairObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hairObj.name = "Hair";
            hairObj.transform.SetParent(head);
            hairObj.transform.localPosition = new Vector3(0f, 0.15f, -0.2f);
            hairObj.transform.localScale = new Vector3(0.95f, 0.65f, 0.85f);
            StripCollider(hairObj);
            hairObj.GetComponent<Renderer>().sharedMaterial = hairMat;

            // --- LEFT ARM ---
            GameObject lArmPivot = new GameObject("LeftArmPivot");
            lArmPivot.transform.SetParent(torso);
            lArmPivot.transform.localPosition = new Vector3(-0.35f, 0.32f, 0f);
            leftArmPivot = lArmPivot.transform;

            GameObject lArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lArm.name = "LeftArm";
            lArm.transform.SetParent(leftArmPivot);
            lArm.transform.localPosition = new Vector3(0f, -0.24f, 0f);
            lArm.transform.localScale = new Vector3(0.24f, 0.26f, 0.24f);
            StripCollider(lArm);
            lArm.GetComponent<Renderer>().sharedMaterial = shirtMat;
            leftArm = lArm.transform;

            GameObject lHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lHand.name = "LeftHand";
            lHand.transform.SetParent(leftArm);
            lHand.transform.localPosition = new Vector3(0f, -1.05f, 0f);
            lHand.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            StripCollider(lHand);
            lHand.GetComponent<Renderer>().sharedMaterial = skinMat;

            // --- RIGHT ARM ---
            GameObject rArmPivot = new GameObject("RightArmPivot");
            rArmPivot.transform.SetParent(torso);
            rArmPivot.transform.localPosition = new Vector3(0.35f, 0.32f, 0f);
            rightArmPivot = rArmPivot.transform;

            GameObject rArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rArm.name = "RightArm";
            rArm.transform.SetParent(rightArmPivot);
            rArm.transform.localPosition = new Vector3(0f, -0.24f, 0f);
            rArm.transform.localScale = new Vector3(0.24f, 0.26f, 0.24f);
            StripCollider(rArm);
            rArm.GetComponent<Renderer>().sharedMaterial = shirtMat;
            rightArm = rArm.transform;

            GameObject rHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rHand.name = "RightHand";
            rHand.transform.SetParent(rightArm);
            rHand.transform.localPosition = new Vector3(0f, -1.05f, 0f);
            rHand.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            StripCollider(rHand);
            rHand.GetComponent<Renderer>().sharedMaterial = skinMat;

            // --- LEFT LEG ---
            GameObject lLegPivot = new GameObject("LeftLegPivot");
            lLegPivot.transform.SetParent(transform);
            lLegPivot.transform.localPosition = new Vector3(-0.16f, 0.65f, 0f);
            leftLegPivot = lLegPivot.transform;

            GameObject lThigh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lThigh.name = "LeftThigh";
            lThigh.transform.SetParent(leftLegPivot);
            lThigh.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            lThigh.transform.localScale = new Vector3(0.25f, 0.22f, 0.25f);
            StripCollider(lThigh);
            lThigh.GetComponent<Renderer>().sharedMaterial = pantsMat;
            leftLeg = lThigh.transform;

            GameObject lCalfObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lCalfObj.name = "LeftCalf";
            lCalfObj.transform.SetParent(leftLeg);
            lCalfObj.transform.localPosition = new Vector3(0f, -1.2f, 0f);
            lCalfObj.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);
            StripCollider(lCalfObj);
            lCalfObj.GetComponent<Renderer>().sharedMaterial = pantsMat;
            leftCalf = lCalfObj.transform;

            GameObject lBoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lBoot.name = "LeftBoot";
            lBoot.transform.SetParent(leftCalf);
            lBoot.transform.localPosition = new Vector3(0f, -1.05f, 0.22f);
            lBoot.transform.localScale = new Vector3(1.1f, 0.65f, 1.6f);
            StripCollider(lBoot);
            lBoot.GetComponent<Renderer>().sharedMaterial = bootsMat;

            // --- RIGHT LEG ---
            GameObject rLegPivot = new GameObject("RightLegPivot");
            rLegPivot.transform.SetParent(transform);
            rLegPivot.transform.localPosition = new Vector3(0.16f, 0.65f, 0f);
            rightLegPivot = rLegPivot.transform;

            GameObject rThigh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rThigh.name = "RightThigh";
            rThigh.transform.SetParent(rightLegPivot);
            rThigh.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            rThigh.transform.localScale = new Vector3(0.25f, 0.22f, 0.25f);
            StripCollider(rThigh);
            rThigh.GetComponent<Renderer>().sharedMaterial = pantsMat;
            rightLeg = rThigh.transform;

            GameObject rCalfObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rCalfObj.name = "RightCalf";
            rCalfObj.transform.SetParent(rightLeg);
            rCalfObj.transform.localPosition = new Vector3(0f, -1.2f, 0f);
            rCalfObj.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);
            StripCollider(rCalfObj);
            rCalfObj.GetComponent<Renderer>().sharedMaterial = pantsMat;
            rightCalf = rCalfObj.transform;

            GameObject rBoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rBoot.name = "RightBoot";
            rBoot.transform.SetParent(rightCalf);
            rBoot.transform.localPosition = new Vector3(0f, -1.05f, 0.22f);
            rBoot.transform.localScale = new Vector3(1.1f, 0.65f, 1.6f);
            StripCollider(rBoot);
            rBoot.GetComponent<Renderer>().sharedMaterial = bootsMat;
        }

        private void Update()
        {
            if (torso == null)
            {
                BuildHumanBody();
                if (torso == null) return;
            }

            if (playerController == null)
            {
                playerController = GetComponentInParent<PlayerController>();
                if (playerController == null) return;
            }

            if (playerController.IsDead)
            {
                AnimateDeath();
                return;
            }

            if (playerController.IsSliding)
            {
                AnimateSlide();
                return;
            }

            if (!playerController.IsGrounded)
            {
                AnimateJump();
                return;
            }

            AnimateRun();
        }

        private void AnimateRun()
        {
            float speed = playerController != null ? playerController.CurrentSpeed : 12f;
            float strideFreq = Mathf.Lerp(12f, 18f, (speed - 10f) / 14f);
            runPhase += strideFreq * Time.deltaTime;

            float legAngle = Mathf.Sin(runPhase) * 42f;
            float armAngle = -Mathf.Sin(runPhase) * 45f;
            float bounce = Mathf.Abs(Mathf.Sin(runPhase * 2f)) * 0.08f;

            // Torso lean forward and subtle vertical bounce
            torso.localPosition = new Vector3(0f, 0.95f + bounce, 0f);
            torso.localRotation = Quaternion.Euler(9f, 0f, 0f);

            // Legs swinging forward/backward with natural knee bend on backswing
            leftLegPivot.localRotation = Quaternion.Euler(legAngle, 0f, 0f);
            rightLegPivot.localRotation = Quaternion.Euler(-legAngle, 0f, 0f);

            float lKnee = Mathf.Clamp(-Mathf.Sin(runPhase), 0f, 1f) * 45f;
            float rKnee = Mathf.Clamp(Mathf.Sin(runPhase), 0f, 1f) * 45f;
            leftCalf.localRotation = Quaternion.Euler(lKnee, 0f, 0f);
            rightCalf.localRotation = Quaternion.Euler(rKnee, 0f, 0f);

            // Arms swinging opposite to legs
            leftArmPivot.localRotation = Quaternion.Euler(armAngle, 0f, 12f);
            rightArmPivot.localRotation = Quaternion.Euler(-armAngle, 0f, -12f);

            // Head stays focused forward
            head.localRotation = Quaternion.Euler(-6f, 0f, 0f);
        }

        private void AnimateJump()
        {
            // Tucked legs and raised arms for athletic leap
            torso.localPosition = new Vector3(0f, 0.92f, 0f);
            torso.localRotation = Quaternion.Euler(-4f, 0f, 0f);

            leftLegPivot.localRotation = Quaternion.Euler(38f, 0f, 0f);
            rightLegPivot.localRotation = Quaternion.Euler(25f, 0f, 0f);
            leftCalf.localRotation = Quaternion.Euler(55f, 0f, 0f);
            rightCalf.localRotation = Quaternion.Euler(45f, 0f, 0f);

            leftArmPivot.localRotation = Quaternion.Euler(-65f, 0f, 25f);
            rightArmPivot.localRotation = Quaternion.Euler(-60f, 0f, -25f);
            head.localRotation = Quaternion.Euler(-10f, 0f, 0f);
        }

        private void AnimateSlide()
        {
            // Low athletic baseball slide under high obstacles
            torso.localPosition = new Vector3(0f, 0.42f, 0.15f);
            torso.localRotation = Quaternion.Euler(45f, 0f, 0f);

            leftLegPivot.localRotation = Quaternion.Euler(-65f, 0f, 0f);
            rightLegPivot.localRotation = Quaternion.Euler(35f, 0f, 0f);
            leftCalf.localRotation = Quaternion.Euler(15f, 0f, 0f);
            rightCalf.localRotation = Quaternion.Euler(75f, 0f, 0f);

            leftArmPivot.localRotation = Quaternion.Euler(30f, 0f, 35f);
            rightArmPivot.localRotation = Quaternion.Euler(40f, 0f, -35f);
            head.localRotation = Quaternion.Euler(-25f, 0f, 0f);
        }

        private void AnimateDeath()
        {
            torso.localPosition = new Vector3(0f, 0.25f, -0.4f);
            torso.localRotation = Quaternion.Euler(-75f, 0f, 20f);
            leftLegPivot.localRotation = Quaternion.Euler(20f, 0f, 0f);
            rightLegPivot.localRotation = Quaternion.Euler(40f, 0f, 0f);
            leftArmPivot.localRotation = Quaternion.Euler(-45f, 0f, 40f);
            rightArmPivot.localRotation = Quaternion.Euler(-30f, 0f, -40f);
        }

        private Material MakeMat(Shader s, Color c, float metallic, float smoothness)
        {
            Material m = new Material(s);
            m.SetColor("_BaseColor", c);
            m.SetColor("_Color", c);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        private void StripCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying)
                    Destroy(col);
                else
                    DestroyImmediate(col);
            }
        }
    }
}
