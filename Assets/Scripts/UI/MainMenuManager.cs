using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DinosaurRunner.Audio;

namespace DinosaurRunner.UI
{
    /// <summary>
    /// Controls the Main Menu scene: Play game, view controls/instructions,
    /// adjust audio volume, view all-time high score and coins, and quit game.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Main Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button exitButton;

        [Header("Text Displays")]
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI totalCoinsText;

        [Header("Modals")]
        [SerializeField] private GameObject controlsModal;
        [SerializeField] private Button closeControlsButton;

        [Header("Audio Sliders")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        private const string BestScoreKey = "DinoRunner_BestScore";
        private const string TotalCoinsKey = "DinoRunner_TotalCoins";

        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private bool showControlsGUI;

        private void Start()
        {
            Time.timeScale = 1f;

            int bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
            int totalCoins = PlayerPrefs.GetInt(TotalCoinsKey, 0);

            if (bestScoreText != null) bestScoreText.text = $"BEST DISTANCE: {bestScore}m";
            if (totalCoinsText != null) totalCoinsText.text = $"TOTAL COINS: {totalCoins}";

            if (playButton != null) playButton.onClick.AddListener(PlayGame);
            if (controlsButton != null) controlsButton.onClick.AddListener(OpenControls);
            if (exitButton != null) exitButton.onClick.AddListener(QuitGame);
            if (closeControlsButton != null) closeControlsButton.onClick.AddListener(CloseControls);

            if (musicSlider != null && AudioManager.Instance != null)
            {
                musicSlider.value = AudioManager.Instance.MusicVolume;
                musicSlider.onValueChanged.AddListener(val => AudioManager.Instance.SetMusicVolume(val));
            }

            if (sfxSlider != null && AudioManager.Instance != null)
            {
                sfxSlider.value = AudioManager.Instance.SfxVolume;
                sfxSlider.onValueChanged.AddListener(val => AudioManager.Instance.SetSfxVolume(val));
            }
        }

        public void PlayGame()
        {
            AudioManager.Instance?.PlayButtonSFX();
            SceneManager.LoadScene("DinosaurLevel");
        }

        public void OpenControls()
        {
            AudioManager.Instance?.PlayButtonSFX();
            if (controlsModal != null) controlsModal.SetActive(true);
            showControlsGUI = true;
        }

        public void CloseControls()
        {
            AudioManager.Instance?.PlayButtonSFX();
            if (controlsModal != null) controlsModal.SetActive(false);
            showControlsGUI = false;
        }

        public void QuitGame()
        {
            AudioManager.Instance?.PlayButtonSFX();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // OnGUI fallback to guarantee interactive UI in Main Menu
        private void OnGUI()
        {
            if (playButton != null && playButton.gameObject.activeInHierarchy)
                return;

            InitStyles();

            int bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
            int totalCoins = PlayerPrefs.GetInt(TotalCoinsKey, 0);

            // Title
            GUI.Label(new Rect(Screen.width / 2 - 250, Screen.height * 0.12f, 500, 60), "DINOSAUR RUNNER", titleStyle);

            // Stats box
            GUI.Box(new Rect(Screen.width / 2 - 160, Screen.height * 0.26f, 320, 75), GUIContent.none);
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height * 0.27f, 300, 30), $"BEST DISTANCE: {bestScore}m", labelStyle);
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height * 0.31f, 300, 30), $"TOTAL COINS: {totalCoins}", labelStyle);

            // Main Buttons
            float btnY = Screen.height * 0.44f;
            float btnW = 260f;
            float btnH = 55f;
            float btnX = Screen.width / 2 - btnW / 2;

            if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "PLAY GAME", buttonStyle))
            {
                PlayGame();
            }

            if (GUI.Button(new Rect(btnX, btnY + 70, btnW, btnH), "CONTROLS", buttonStyle))
            {
                showControlsGUI = !showControlsGUI;
            }

            if (GUI.Button(new Rect(btnX, btnY + 140, btnW, btnH), "EXIT", buttonStyle))
            {
                QuitGame();
            }

            // Controls Modal Dialog
            if (showControlsGUI)
            {
                GUI.Box(new Rect(Screen.width / 2 - 220, Screen.height / 2 - 160, 440, 320), GUIContent.none);
                GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 145, 400, 40), "GAMEPLAY CONTROLS", titleStyle);

                string controlsText = "A / Left Arrow: Move Left Lane\n" +
                                      "D / Right Arrow: Move Right Lane\n" +
                                      "Spacebar / Up: Jump Over Logs\n" +
                                      "S / Down Arrow: Slide Under Obstacles\n" +
                                      "Escape: Pause Game\n\n" +
                                      "Avoid obstacles, dodge dinosaurs & collect gold coins!";

                GUI.Label(new Rect(Screen.width / 2 - 190, Screen.height / 2 - 95, 380, 170), controlsText, labelStyle);

                if (GUI.Button(new Rect(Screen.width / 2 - 80, Screen.height / 2 + 85, 160, 45), "CLOSE", buttonStyle))
                {
                    showControlsGUI = false;
                }
            }
        }

        private void InitStyles()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 36,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                titleStyle.normal.textColor = new Color(1f, 0.82f, 0.2f);

                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                labelStyle.normal.textColor = Color.white;

                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold
                };
            }
        }
    }
}
