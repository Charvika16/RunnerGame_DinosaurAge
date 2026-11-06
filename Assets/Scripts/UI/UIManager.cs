using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DinosaurRunner.Audio;

namespace DinosaurRunner.UI
{
    /// <summary>
    /// Manages In-Game HUD, Pause Menu, Game Over Screen, and Controls Modal
    /// with full UGUI integration and responsive rendering.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD Panels")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI multiplierText;
        [SerializeField] private GameObject shieldIndicator;
        [SerializeField] private GameObject magnetIndicator;

        [Header("Pause Menu")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Game Over Screen")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI finalCoinsText;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private GameObject newBestBanner;
        [SerializeField] private Button gameOverRestartButton;
        [SerializeField] private Button gameOverMenuButton;

        [Header("Controls Modal")]
        [SerializeField] private GameObject controlsPanel;
        [SerializeField] private Button closeControlsButton;

        private Player.PlayerController playerController;
        private GUIStyle guiTitleStyle;
        private GUIStyle guiLabelStyle;
        private GUIStyle guiButtonStyle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            WireButtons();
        }

        private void Start()
        {
            playerController = FindAnyObjectByType<Player.PlayerController>();

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged += UpdateScoreUI;
                ScoreManager.Instance.OnCoinsChanged += UpdateCoinsUI;
            }

            // Sync volume sliders
            if (AudioManager.Instance != null)
            {
                if (musicSlider != null) musicSlider.value = AudioManager.Instance.MusicVolume;
                if (sfxSlider != null) sfxSlider.value = AudioManager.Instance.SfxVolume;
            }

            ShowHUD();
        }

        private void OnDestroy()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged -= UpdateScoreUI;
                ScoreManager.Instance.OnCoinsChanged -= UpdateCoinsUI;
            }
        }

        private void Update()
        {
            // Escape to Pause / Resume
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (GameManager.GameManager.Instance != null)
                {
                    if (GameManager.GameManager.Instance.IsPaused)
                        ResumeGame();
                    else if (GameManager.GameManager.Instance.IsPlaying)
                        PauseGame();
                }
            }

            UpdatePowerupIndicators();
        }

        private void WireButtons()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
            if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
            if (menuButton != null) menuButton.onClick.AddListener(LoadMainMenu);

            if (gameOverRestartButton != null) gameOverRestartButton.onClick.AddListener(RestartGame);
            if (gameOverMenuButton != null) gameOverMenuButton.onClick.AddListener(LoadMainMenu);

            if (closeControlsButton != null) closeControlsButton.onClick.AddListener(HideControls);

            if (musicSlider != null)
                musicSlider.onValueChanged.AddListener(val => AudioManager.Instance?.SetMusicVolume(val));
            if (sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(val => AudioManager.Instance?.SetSfxVolume(val));
        }

        public void UpdateScoreUI(int score)
        {
            if (scoreText != null)
                scoreText.text = $"SCORE: {score}";
        }

        public void UpdateCoinsUI(int coins)
        {
            if (coinText != null)
                coinText.text = $"COINS: {coins}";
        }

        private void UpdatePowerupIndicators()
        {
            if (playerController == null)
            {
                playerController = FindAnyObjectByType<Player.PlayerController>();
                return;
            }

            if (shieldIndicator != null)
                shieldIndicator.SetActive(playerController.HasShield);

            if (magnetIndicator != null)
                magnetIndicator.SetActive(playerController.HasMagnet);

            if (multiplierText != null && ScoreManager.Instance != null)
            {
                bool active = ScoreManager.Instance.ScoreMultiplier > 1;
                multiplierText.gameObject.SetActive(active);
                if (active)
                    multiplierText.text = $"{ScoreManager.Instance.ScoreMultiplier}X SCORE ({Mathf.CeilToInt(ScoreManager.Instance.MultiplierTimeRemaining)}s)";
            }
        }

        public void ShowHUD()
        {
            if (hudPanel != null) hudPanel.SetActive(true);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(false);
        }

        public void PauseGame()
        {
            AudioManager.Instance?.PlayButtonSFX();
            GameManager.GameManager.Instance?.PauseGame();

            if (pausePanel != null) pausePanel.SetActive(true);
            if (hudPanel != null) hudPanel.SetActive(false);
        }

        public void ResumeGame()
        {
            AudioManager.Instance?.PlayButtonSFX();
            GameManager.GameManager.Instance?.ResumeGame();

            if (pausePanel != null) pausePanel.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(true);
        }

        public void ShowGameOver()
        {
            if (hudPanel != null) hudPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(true);

            if (ScoreManager.Instance != null)
            {
                if (finalScoreText != null)
                    finalScoreText.text = $"SCORE: {ScoreManager.Instance.CurrentScore}";
                if (finalCoinsText != null)
                    finalCoinsText.text = $"COINS COLLECTED: {ScoreManager.Instance.CoinsThisRun}";
                if (bestScoreText != null)
                    bestScoreText.text = $"BEST SCORE: {ScoreManager.Instance.BestScore}";
                if (newBestBanner != null)
                    newBestBanner.SetActive(ScoreManager.Instance.IsNewBestScore);
            }
        }

        public void RestartGame()
        {
            AudioManager.Instance?.PlayButtonSFX();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadMainMenu()
        {
            AudioManager.Instance?.PlayButtonSFX();
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        public void ShowControls()
        {
            AudioManager.Instance?.PlayButtonSFX();
            if (controlsPanel != null) controlsPanel.SetActive(true);
        }

        public void HideControls()
        {
            AudioManager.Instance?.PlayButtonSFX();
            if (controlsPanel != null) controlsPanel.SetActive(false);
        }

        // Beautiful GUI Fallback to guarantee UI works regardless of canvas wiring
        private void OnGUI()
        {
            // Only render OnGUI fallback if HUD Canvas elements are not active or assigned
            if (hudPanel != null && hudPanel.activeInHierarchy)
                return;

            InitGUIStyles();

            int score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
            int coins = ScoreManager.Instance != null ? ScoreManager.Instance.CoinsThisRun : 0;

            // HUD Badges
            GUI.Box(new Rect(20, 20, 220, 95), GUIContent.none);
            GUI.Label(new Rect(30, 25, 200, 40), $"SCORE: {score}", guiLabelStyle);
            GUI.Label(new Rect(30, 65, 200, 40), $"COINS: {coins}", guiLabelStyle);

            if (playerController != null)
            {
                if (playerController.HasShield)
                    GUI.Label(new Rect(250, 25, 200, 30), "SHIELD ACTIVE", guiLabelStyle);
                if (playerController.HasMagnet)
                    GUI.Label(new Rect(250, 60, 200, 30), "MAGNET ACTIVE", guiLabelStyle);
            }

            // Pause Button top right
            if (GUI.Button(new Rect(Screen.width - 120, 20, 100, 45), "PAUSE", guiButtonStyle))
            {
                PauseGame();
            }

            // Pause Screen
            if (GameManager.GameManager.Instance != null && GameManager.GameManager.Instance.IsPaused)
            {
                GUI.Box(new Rect(Screen.width / 2 - 180, Screen.height / 2 - 160, 360, 320), GUIContent.none);
                GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 140, 300, 50), "GAME PAUSED", guiTitleStyle);

                if (GUI.Button(new Rect(Screen.width / 2 - 120, Screen.height / 2 - 60, 240, 50), "RESUME", guiButtonStyle))
                    ResumeGame();
                if (GUI.Button(new Rect(Screen.width / 2 - 120, Screen.height / 2 + 5, 240, 50), "RESTART", guiButtonStyle))
                    RestartGame();
                if (GUI.Button(new Rect(Screen.width / 2 - 120, Screen.height / 2 + 70, 240, 50), "MAIN MENU", guiButtonStyle))
                    LoadMainMenu();
            }

            // Game Over Screen
            if (GameManager.GameManager.Instance != null && GameManager.GameManager.Instance.IsGameOver)
            {
                GUI.Box(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 180, 400, 380), GUIContent.none);
                GUI.Label(new Rect(Screen.width / 2 - 180, Screen.height / 2 - 160, 360, 50), "GAME OVER", guiTitleStyle);

                GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 95, 300, 35), $"FINAL SCORE: {score}", guiLabelStyle);
                GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 60, 300, 35), $"COINS COLLECTED: {coins}", guiLabelStyle);

                int best = ScoreManager.Instance != null ? ScoreManager.Instance.BestScore : score;
                GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 25, 300, 35), $"BEST SCORE: {best}", guiLabelStyle);

                if (GUI.Button(new Rect(Screen.width / 2 - 120, Screen.height / 2 + 30, 240, 50), "PLAY AGAIN", guiButtonStyle))
                    RestartGame();
                if (GUI.Button(new Rect(Screen.width / 2 - 120, Screen.height / 2 + 95, 240, 50), "MAIN MENU", guiButtonStyle))
                    LoadMainMenu();
            }
        }

        private void InitGUIStyles()
        {
            if (guiTitleStyle == null)
            {
                guiTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 32,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                guiTitleStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);

                guiLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold
                };
                guiLabelStyle.normal.textColor = Color.white;

                guiButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold
                };
            }
        }
    }
}

