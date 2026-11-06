using UnityEngine;

namespace DinosaurRunner.CameraSystem
{
    /// <summary>
    /// Smooth third-person follow camera maintaining optimal distance,
    /// forward lookahead, and subtle impact shake.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target & Offsets")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 3.2f, -5.2f);
        [SerializeField] private Vector3 lookAheadOffset = new Vector3(0f, 0.4f, 5.0f);

        [Header("Follow Dynamics")]
        [SerializeField] private float smoothSpeedX = 6f;
        [SerializeField] private float smoothSpeedY = 10f;

        [Header("Camera Shake")]
        [SerializeField] private float defaultShakeDuration = 0.35f;
        [SerializeField] private float defaultShakeMagnitude = 0.25f;

        private float currentX;
        private float shakeTimeRemaining;
        private float shakeMagnitude;

        private void Start()
        {
            FindTarget();
            if (target != null)
            {
                currentX = target.position.x;
                SnapToTarget();
            }
        }

        private void FindTarget()
        {
            if (target == null)
            {
                var player = FindAnyObjectByType<Player.PlayerController>();
                if (player != null)
                    target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                FindTarget();
                return;
            }

            // Smooth lateral X tracking
            currentX = Mathf.Lerp(currentX, target.position.x, smoothSpeedX * Time.deltaTime);

            // Follow Y with smooth clamp
            float targetY = Mathf.Max(0.5f, target.position.y);
            float currentY = Mathf.Lerp(transform.position.y - offset.y, targetY, smoothSpeedY * Time.deltaTime);

            Vector3 desiredPosition = new Vector3(
                currentX + offset.x,
                currentY + offset.y,
                target.position.z + offset.z
            );

            // Apply camera shake if active
            if (shakeTimeRemaining > 0f)
            {
                shakeTimeRemaining -= Time.deltaTime;
                Vector3 shakeOffset = Random.insideUnitSphere * shakeMagnitude;
                desiredPosition += shakeOffset;
            }

            transform.position = desiredPosition;

            // Look ahead along running path at character height
            Vector3 lookTarget = new Vector3(currentX, 1.0f, target.position.z) + lookAheadOffset;
            transform.LookAt(lookTarget);
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            currentX = target.position.x;
            transform.position = target.position + offset;
            transform.LookAt(target.position + lookAheadOffset);
        }

        public void TriggerShake(float duration = -1f, float magnitude = -1f)
        {
            shakeTimeRemaining = duration > 0 ? duration : defaultShakeDuration;
            shakeMagnitude = magnitude > 0 ? magnitude : defaultShakeMagnitude;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            SnapToTarget();
        }
    }
}

