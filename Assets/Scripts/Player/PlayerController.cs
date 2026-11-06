using UnityEngine;
using DinosaurRunner.Audio;

namespace DinosaurRunner.Player
{
    /// <summary>
    /// Controls 3-lane horizontal movement, forward running with progressive speed,
    /// jumping, sliding (with collider shrinking), ground detection, and power-up states.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Lane Settings")]
        [SerializeField] private float laneDistance = 2.5f;
        [SerializeField] private float laneChangeSpeed = 16f;

        [Header("Running & Speed")]
        [SerializeField] private float initialSpeed = 10f;
        [SerializeField] private float maxSpeed = 24f;
        [SerializeField] private float speedIncreaseRate = 0.12f;

        [Header("Jump Physics")]
        [SerializeField] private float jumpForce = 9.5f;
        [SerializeField] private float gravity = 28f;
        [SerializeField] private float fastFallMultiplier = 1.8f;

        [Header("Slide Settings")]
        [SerializeField] private float slideDuration = 0.8f;
        [SerializeField] private float normalHeight = 1.8f;
        [SerializeField] private float normalCenterY = 0.9f;
        [SerializeField] private float slideHeight = 0.7f;
        [SerializeField] private float slideCenterY = 0.35f;

        [Header("Magnet Power-up")]
        [SerializeField] private float magnetRadius = 12f;

        [Header("Visual & FX")]
        [SerializeField] private GameObject shieldVisual;
        [SerializeField] private GameObject magnetVisual;

        // Runtime states
        public float CurrentSpeed { get; private set; }
        public int CurrentLane { get; private set; } = 1; // 0=Left, 1=Center, 2=Right
        public bool IsGrounded => characterController.isGrounded || transform.position.y <= 0.05f;
        public bool IsSliding { get; private set; }
        public bool IsDead { get; private set; }
        public bool HasShield => shieldTimer > 0f;
        public bool HasMagnet => magnetTimer > 0f;
        public float MagnetRadius => magnetRadius;
        public float ShieldTimeRemaining => shieldTimer;
        public float MagnetTimeRemaining => magnetTimer;

        private CharacterController characterController;
        private PlayerAnimationController animController;
        private float verticalVelocity;
        private float slideTimer;
        private float targetX;
        private float shieldTimer;
        private float magnetTimer;
        private float footstepTimer;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            animController = GetComponent<PlayerAnimationController>();
            CurrentSpeed = initialSpeed;
            targetX = 0f;
            CurrentLane = 1;

            if (characterController != null)
            {
                characterController.height = normalHeight;
                characterController.center = new Vector3(0, normalCenterY, 0);
            }
        }

        private void Update()
        {
            if (IsDead)
                return;

            HandleInput();
            UpdateTimers();
            MoveCharacter();
            UpdateFootsteps();
        }

        private void HandleInput()
        {
            // Lane Switch Left (A or Left Arrow)
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                SwitchLane(-1);
            }

            // Lane Switch Right (D or Right Arrow)
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                SwitchLane(1);
            }

            // Jump (Space or Up Arrow or W)
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                Jump();
            }

            // Slide (S or Down Arrow)
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                Slide();
            }
        }

        private void SwitchLane(int direction)
        {
            int newLane = Mathf.Clamp(CurrentLane + direction, 0, 2);
            if (newLane != CurrentLane)
            {
                CurrentLane = newLane;
                targetX = (CurrentLane - 1) * laneDistance;
            }
        }

        private void Jump()
        {
            if (IsGrounded && !IsSliding)
            {
                verticalVelocity = jumpForce;
                if (animController != null)
                    animController.TriggerJump();

                AudioManager.Instance?.PlayJumpSFX();
            }
        }

        private void Slide()
        {
            if (!IsGrounded)
            {
                // Fast fall downward when pressing down in air
                verticalVelocity = -jumpForce * fastFallMultiplier;
            }

            if (!IsSliding)
            {
                IsSliding = true;
                slideTimer = slideDuration;

                // Adjust collider height for slide
                if (characterController != null)
                {
                    characterController.height = slideHeight;
                    characterController.center = new Vector3(0, slideCenterY, 0);
                }

                if (animController != null)
                    animController.TriggerSlide();

                AudioManager.Instance?.PlaySlideSFX();
            }
            else
            {
                // Refresh slide timer
                slideTimer = slideDuration;
            }
        }

        private void MoveCharacter()
        {
            // Gradually accelerate forward speed
            CurrentSpeed = Mathf.Min(CurrentSpeed + speedIncreaseRate * Time.deltaTime, maxSpeed);

            // Calculate lateral movement
            Vector3 currentPos = transform.position;
            float newX = Mathf.MoveTowards(currentPos.x, targetX, laneChangeSpeed * Time.deltaTime);

            // Calculate vertical gravity & jumping
            if (IsGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1.5f; // Slight downward pressure to keep grounded
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            // Combine velocities
            Vector3 moveDelta = new Vector3(
                newX - currentPos.x,
                verticalVelocity * Time.deltaTime,
                CurrentSpeed * Time.deltaTime
            );

            characterController.Move(moveDelta);

            // Ground safety clamp
            if (transform.position.y < 0f)
            {
                Vector3 clamped = transform.position;
                clamped.y = 0f;
                transform.position = clamped;
                verticalVelocity = 0f;
            }

            // Check slide duration expiration
            if (IsSliding)
            {
                slideTimer -= Time.deltaTime;
                if (slideTimer <= 0f)
                {
                    EndSlide();
                }
            }

            // Update animation parameters
            if (animController != null)
            {
                animController.SetGrounded(IsGrounded);
                animController.SetSpeed(CurrentSpeed / initialSpeed);
            }
        }

        private void EndSlide()
        {
            IsSliding = false;
            if (characterController != null)
            {
                characterController.height = normalHeight;
                characterController.center = new Vector3(0, normalCenterY, 0);
            }

            if (animController != null)
                animController.EndSlide();
        }

        private void UpdateTimers()
        {
            if (shieldTimer > 0f)
            {
                shieldTimer -= Time.deltaTime;
                if (shieldVisual != null)
                    shieldVisual.SetActive(shieldTimer > 0f);
            }

            if (magnetTimer > 0f)
            {
                magnetTimer -= Time.deltaTime;
                if (magnetVisual != null)
                    magnetVisual.SetActive(magnetTimer > 0f);
            }
        }

        private void UpdateFootsteps()
        {
            if (IsGrounded && !IsSliding && !IsDead)
            {
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0f)
                {
                    AudioManager.Instance?.PlayFootstepSFX();
                    footstepTimer = Mathf.Max(0.22f, 3.2f / CurrentSpeed);
                }
            }
        }

        public void ActivateShield(float duration)
        {
            shieldTimer = Mathf.Max(shieldTimer, duration);
            if (shieldVisual != null)
                shieldVisual.SetActive(true);
            AudioManager.Instance?.PlayPowerupSFX();
        }

        public void ActivateMagnet(float duration)
        {
            magnetTimer = Mathf.Max(magnetTimer, duration);
            if (magnetVisual != null)
                magnetVisual.SetActive(true);
            AudioManager.Instance?.PlayPowerupSFX();
        }

        public void Die()
        {
            if (IsDead)
                return;

            IsDead = true;
            CurrentSpeed = 0f;
            EndSlide();

            if (animController != null)
                animController.TriggerDeath();

            AudioManager.Instance?.PlayImpactSFX();
            AudioManager.Instance?.PlayGameOverSFX();

            GameManager.GameManager.Instance?.OnPlayerDied();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (IsDead)
                return;

            Obstacles.ObstacleController obstacle = hit.gameObject.GetComponent<Obstacles.ObstacleController>();
            if (obstacle != null)
            {
                if (HasShield)
                {
                    // Smash obstacle
                    obstacle.Smash();
                    AudioManager.Instance?.PlayImpactSFX();
                }
                else
                {
                    Die();
                }
            }
        }

        public void ResetPlayer(Vector3 spawnPosition)
        {
            IsDead = false;
            CurrentSpeed = initialSpeed;
            CurrentLane = 1;
            targetX = 0f;
            verticalVelocity = 0f;
            shieldTimer = 0f;
            magnetTimer = 0f;
            if (shieldVisual != null) shieldVisual.SetActive(false);
            if (magnetVisual != null) magnetVisual.SetActive(false);

            characterController.enabled = false;
            transform.position = spawnPosition;
            characterController.enabled = true;

            EndSlide();
            if (animController != null)
                animController.ResetAnimations();
        }
    }
}
