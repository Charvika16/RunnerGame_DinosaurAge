using System.Collections;
using UnityEngine;
using DinosaurRunner.UI;
using DinosaurRunner.Audio;

namespace DinosaurRunner.GameManager
{
    /// <summary>
    /// Handles Game Over state transitions, final score calculation,
    /// PlayerPrefs progress persistence, and triggers UI screen displays.
    /// </summary>
    public class GameOverManager : MonoBehaviour
    {
        [Header("Timing Settings")]
        [SerializeField] private float gameOverDelay = 1.2f;

        public void TriggerGameOver()
        {
            StartCoroutine(GameOverRoutine());
        }

        private IEnumerator GameOverRoutine()
        {
            // Wait brief moment to let player impact / death animation play
            yield return new WaitForSeconds(gameOverDelay);

            // Save persistent score and coins
            ScoreManager.Instance?.SaveProgress();

            // Display Game Over screen
            UIManager.Instance?.ShowGameOver();
        }
    }
}
