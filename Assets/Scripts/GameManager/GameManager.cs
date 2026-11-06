using UnityEngine;
using UnityEngine.SceneManagement;
using DinosaurRunner.Audio;
using DinosaurRunner.UI;

namespace DinosaurRunner.GameManager
{
    public enum GameState
    {
        Starting,
        Playing,
        Paused,
        GameOver
    }

    /// <summary>
    /// Master coordinator for game lifecycle states, timescale,
    /// scene management, and player death handling.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private GameState currentState = GameState.Playing;

        [Header("Subsystem References")]
        [SerializeField] private GameOverManager gameOverManager;

        public GameState CurrentState => currentState;
        public bool IsPlaying => currentState == GameState.Playing;
        public bool IsPaused => currentState == GameState.Paused;
        public bool IsGameOver => currentState == GameState.GameOver;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (gameOverManager == null)
                gameOverManager = GetComponent<GameOverManager>() ?? gameObject.AddComponent<GameOverManager>();

            Time.timeScale = 1f;
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            StartGame();
        }

        public void StartGame()
        {
            currentState = GameState.Playing;
            Time.timeScale = 1f;
            ScoreManager.Instance?.ResetCurrentRun();
        }

        public void PauseGame()
        {
            if (currentState != GameState.Playing) return;
            currentState = GameState.Paused;
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            if (currentState != GameState.Paused) return;
            currentState = GameState.Playing;
            Time.timeScale = 1f;
        }

        public void OnPlayerDied()
        {
            if (currentState == GameState.GameOver) return;
            currentState = GameState.GameOver;

            gameOverManager?.TriggerGameOver();
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
