using UnityEngine;

namespace DinosaurRunner.Player
{
    /// <summary>
    /// Handles player animation states, blending, and visual transforms
    /// (including subtle banking into lane switches and jump/slide visuals).
    /// </summary>
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("Animator")]
        [SerializeField] private Animator animator;

        [Header("Visual Model Reference")]
        [SerializeField] private Transform modelTransform;

        [Header("Procedural Visual Dynamics")]
        [SerializeField] private float rollTiltAmount = 14f;
        [SerializeField] private float tiltSmoothing = 10f;

        // Animator Parameter Hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int SlideHash = Animator.StringToHash("Slide");
        private static readonly int DieHash = Animator.StringToHash("Die");
        private static readonly int ResetHash = Animator.StringToHash("Reset");

        private PlayerController playerController;
        private Vector3 originalModelScale = Vector3.one;
        private Vector3 targetModelScale = Vector3.one;
        private float currentTilt = 0f;

        private bool hasSpeedParam;
        private bool hasGroundedParam;
        private bool hasJumpParam;
        private bool hasSlideParam;
        private bool hasDieParam;
        private bool hasResetParam;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (modelTransform == null && animator != null)
                modelTransform = animator.transform;
            else if (modelTransform == null)
                modelTransform = transform;

            originalModelScale = modelTransform.localScale;
            targetModelScale = originalModelScale;

            CacheParameters();
            FindModelTransform();
        }

        private void Start()
        {
            FindModelTransform();
        }

        private void FindModelTransform()
        {
            HumanRunnerModel human = GetComponentInChildren<HumanRunnerModel>();
            if (human != null)
            {
                modelTransform = human.transform;
                originalModelScale = modelTransform.localScale;
                targetModelScale = originalModelScale;
            }
        }

        private void CacheParameters()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return;

            foreach (var p in animator.parameters)
            {
                if (p.nameHash == SpeedHash) hasSpeedParam = true;
                else if (p.nameHash == IsGroundedHash) hasGroundedParam = true;
                else if (p.nameHash == JumpHash) hasJumpParam = true;
                else if (p.nameHash == SlideHash) hasSlideParam = true;
                else if (p.nameHash == DieHash) hasDieParam = true;
                else if (p.nameHash == ResetHash) hasResetParam = true;
            }
        }

        private void LateUpdate()
        {
            UpdateProceduralTilt();
            UpdateVisualScale();
        }

        private void UpdateProceduralTilt()
        {
            if (modelTransform == null || playerController == null || playerController.IsDead)
                return;

            // Tilt model based on lateral deviation from target lane
            float targetX = (playerController.CurrentLane - 1) * 2.5f;
            float diffX = targetX - transform.position.x;
            float targetTilt = Mathf.Clamp(diffX * rollTiltAmount, -rollTiltAmount, rollTiltAmount);

            currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSmoothing * Time.deltaTime);

            Vector3 euler = modelTransform.localEulerAngles;
            euler.z = -currentTilt;
            modelTransform.localEulerAngles = euler;
        }

        private void UpdateVisualScale()
        {
            if (modelTransform == null) return;
            modelTransform.localScale = Vector3.Lerp(modelTransform.localScale, targetModelScale, 15f * Time.deltaTime);
        }

        public void SetSpeed(float speedMultiplier)
        {
            if (animator != null && animator.enabled && hasSpeedParam)
            {
                animator.SetFloat(SpeedHash, speedMultiplier);
            }
        }

        public void SetGrounded(bool isGrounded)
        {
            if (animator != null && animator.enabled && hasGroundedParam)
            {
                animator.SetBool(IsGroundedHash, isGrounded);
            }
        }

        public void TriggerJump()
        {
            if (animator != null && animator.enabled && hasJumpParam)
            {
                animator.SetTrigger(JumpHash);
            }

            // Visual squash/stretch
            targetModelScale = new Vector3(originalModelScale.x * 0.85f, originalModelScale.y * 1.25f, originalModelScale.z * 0.85f);
            CancelInvoke(nameof(RestoreScale));
            Invoke(nameof(RestoreScale), 0.25f);
        }

        public void TriggerSlide()
        {
            if (animator != null && animator.enabled && hasSlideParam)
            {
                animator.SetBool(SlideHash, true);
            }

            // Visual slide crouch
            targetModelScale = new Vector3(originalModelScale.x * 1.15f, originalModelScale.y * 0.55f, originalModelScale.z * 1.25f);
        }

        public void EndSlide()
        {
            if (animator != null && animator.enabled && hasSlideParam)
            {
                animator.SetBool(SlideHash, false);
            }

            RestoreScale();
        }

        private void RestoreScale()
        {
            targetModelScale = originalModelScale;
        }

        public void TriggerDeath()
        {
            if (animator != null && animator.enabled && hasDieParam)
            {
                animator.SetTrigger(DieHash);
            }

            // Tip over backward/sideways on death
            if (modelTransform != null)
            {
                modelTransform.localEulerAngles = new Vector3(-65f, 0f, 15f);
            }
        }

        public void ResetAnimations()
        {
            if (animator != null && animator.enabled)
            {
                if (hasResetParam) animator.SetTrigger(ResetHash);
                if (hasSlideParam) animator.SetBool(SlideHash, false);
                if (hasGroundedParam) animator.SetBool(IsGroundedHash, true);
                if (hasSpeedParam) animator.SetFloat(SpeedHash, 1f);
            }

            currentTilt = 0f;
            targetModelScale = originalModelScale;
            if (modelTransform != null)
            {
                modelTransform.localScale = originalModelScale;
                modelTransform.localRotation = Quaternion.identity;
            }
        }
    }
}
