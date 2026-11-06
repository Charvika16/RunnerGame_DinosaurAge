using System;
using UnityEngine;

namespace DinosaurRunner.UI
{
    /// <summary>
    /// Tracks run distance score, coins collected, power-up multipliers,
    /// and persists all-time best score and total coin bank with PlayerPrefs.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        private const string BestScoreKey = "DinoRunner_BestScore";
        private const string TotalCoinsKey = "DinoRunner_TotalCoins";

        public int CurrentScore { get; private set; }
        public int CoinsThisRun { get; private set; }
        public int BestScore { get; private set; }
        public int TotalCoins { get; private set; }
        public bool IsNewBestScore { get; private set; }
        public int ScoreMultiplier { get; private set; } = 1;
        public float MultiplierTimeRemaining => multiplierTimer;

        public event Action<int> OnScoreChanged;
        public event Action<int> OnCoinsChanged;
        public event Action<int> OnBestScoreChanged;

        private Transform playerTransform;
        private float startPlayerZ;
        private float multiplierTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            LoadSavedData();
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

            // Update Multiplier Timer
            if (multiplierTimer > 0f)
            {
                multiplierTimer -= Time.deltaTime;
                if (multiplierTimer <= 0f)
                {
                    ScoreMultiplier = 1;
                }
            }

            // Calculate Distance Score
            float distance = Mathf.Max(0f, playerTransform.position.z - startPlayerZ);
            int newScore = Mathf.FloorToInt(distance * ScoreMultiplier);

            if (newScore != CurrentScore)
            {
                CurrentScore = newScore;
                OnScoreChanged?.Invoke(CurrentScore);

                if (CurrentScore > BestScore)
                {
                    BestScore = CurrentScore;
                    IsNewBestScore = true;
                    OnBestScoreChanged?.Invoke(BestScore);
                }
            }
        }

        public void AddCoins(int amount)
        {
            CoinsThisRun += amount;
            TotalCoins += amount;
            OnCoinsChanged?.Invoke(CoinsThisRun);
        }

        public void ActivateMultiplier(int multiplier, float duration)
        {
            ScoreMultiplier = multiplier;
            multiplierTimer = Mathf.Max(multiplierTimer, duration);
        }

        public void LoadSavedData()
        {
            BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
            TotalCoins = PlayerPrefs.GetInt(TotalCoinsKey, 0);
        }

        public void SaveProgress()
        {
            if (CurrentScore > PlayerPrefs.GetInt(BestScoreKey, 0))
            {
                PlayerPrefs.SetInt(BestScoreKey, CurrentScore);
            }

            PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins);
            PlayerPrefs.Save();
        }

        public void ResetCurrentRun()
        {
            CurrentScore = 0;
            CoinsThisRun = 0;
            IsNewBestScore = false;
            ScoreMultiplier = 1;
            multiplierTimer = 0f;

            if (playerTransform != null)
                startPlayerZ = playerTransform.position.z;

            OnScoreChanged?.Invoke(0);
            OnCoinsChanged?.Invoke(0);
        }
    }
}

