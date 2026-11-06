using UnityEngine;

namespace DinosaurRunner.Dinosaurs
{
    public enum DinosaurSpecies
    {
        Stegosaurus,
        Velociraptor,
        Pachycephalosaurus
    }

    /// <summary>
    /// Controls 3D dinosaur animations, roars, proximity reactions to the player,
    /// and spatial audio vocalizations.
    /// </summary>
    public class DinosaurController : MonoBehaviour
    {
        [Header("Species Configuration")]
        [SerializeField] private DinosaurSpecies species = DinosaurSpecies.Stegosaurus;
        [SerializeField] private float roarDistance = 16f;
        [SerializeField] private AudioClip[] vocalClips;

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private AudioSource audioSource;

        private Transform playerTransform;
        private bool hasRoared;
        private float nextRoarTime;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.spatialBlend = 1.0f; // Full 3D spatial audio
                    audioSource.minDistance = 3f;
                    audioSource.maxDistance = 35f;
                    audioSource.rolloffMode = AudioRolloffMode.Linear;
                    audioSource.playOnAwake = false;
                }
            }
        }

        private void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                var player = FindAnyObjectByType<Player.PlayerController>();
                if (player != null)
                    playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);

            // Proximity roar reaction
            if (!hasRoared && dist < roarDistance && Time.time > nextRoarTime)
            {
                Roar();
            }
            else if (dist > roarDistance * 1.5f)
            {
                // Reset roar trigger when player moves past or away
                hasRoared = false;
            }
        }

        public void Roar()
        {
            hasRoared = true;
            nextRoarTime = Time.time + Random.Range(4f, 8f);

            // Trigger animation if parameter exists in controller
            if (animator != null && animator.enabled && animator.runtimeAnimatorController != null)
            {
                foreach (var param in animator.parameters)
                {
                    if (param.name == "Roar" && param.type == AnimatorControllerParameterType.Trigger)
                    {
                        animator.SetTrigger("Roar");
                        break;
                    }
                }
            }

            // Play random vocal sound effect
            if (vocalClips != null && vocalClips.Length > 0 && audioSource != null)
            {
                AudioClip clip = vocalClips[Random.Range(0, vocalClips.Length)];
                if (clip != null)
                {
                    audioSource.pitch = Random.Range(0.85f, 1.15f);
                    audioSource.PlayOneShot(clip, 0.85f);
                }
            }
        }

        public void SetSpecies(DinosaurSpecies newSpecies, AudioClip[] clips)
        {
            species = newSpecies;
            vocalClips = clips;
        }

        public void ResetDinosaur()
        {
            hasRoared = false;
            nextRoarTime = 0f;
            FindPlayer();
        }
    }
}

