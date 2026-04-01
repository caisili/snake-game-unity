using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager I { get; private set; }

    [Header("Panels")]
    public GameObject startPanel;
    public GameObject hudPanel;
    public GameObject pausePanel;

    [Header("UI Text (TMP)")]
    public TMP_Text scoreText;
    public TMP_Text gameOverText;

    private void Awake()
    {
        // 单例：场景里只允许一个 UIManager
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
    }

    private void Start()
    {
        ShowStart();
        SetScore(0);
        ShowGameOver(false);
        ShowPause(false);
    }

    public void ShowStart()
    {
        if (startPanel) startPanel.SetActive(true);
        if (hudPanel) hudPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
    }

    public void ShowHUD()
    {
        if (startPanel) startPanel.SetActive(false);
        if (hudPanel) hudPanel.SetActive(true);
        if (pausePanel) pausePanel.SetActive(false);
    }

    public void ShowPause(bool show)
    {
        if (pausePanel) pausePanel.SetActive(show);
    }

    public void SetScore(int score)
    {
        if (scoreText) scoreText.text = $"Score: {score}";
    }

    public void ShowGameOver(bool show)
    {
        if (!gameOverText) return;
        gameOverText.gameObject.SetActive(show);
        if (show) gameOverText.text = "Game Over\nPress R to Restart";
    }

    // ---------- Buttons ----------
    public void OnClickStart()
    {
        AudioManager.I?.PlayClick();
        GameManager.I?.StartGame();
    }

    public void OnClickResume()
    {
        AudioManager.I?.PlayClick();
        GameManager.I?.SetPaused(false);
    }

    public void OnClickRestart()
    {
        AudioManager.I?.PlayClick();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public void OnClickQuit()
    {
        AudioManager.I?.PlayClick();
        Application.Quit();
    }
}