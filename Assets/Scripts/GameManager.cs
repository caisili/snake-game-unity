using System;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    [Header("Grid")]
    public int width = 30;
    public int height = 20;

    [Header("World Mapping")]
    public float cellSize = 1f;                 // 1格=多少世界单位（通常 1）
    public Vector2 worldOrigin = Vector2.zero;  // 网格中心对齐到哪里（通常 0,0）

    [Header("Difficulty (Snake & Enemy share the same step time)")]
    public float baseStepTime = 0.22f;
    public float minStepTime = 0.08f;
    public float stepTimeDecreasePerScore = 0.0035f;

    public bool IsGameOver { get; private set; }
    public bool HasStarted { get; private set; }
    public bool IsPaused { get; private set; }
    public int Score { get; private set; }

    public float CurrentStepTime
    {
        get
        {
            float t = baseStepTime - Score * stepTimeDecreasePerScore;
            return Mathf.Max(minStepTime, t);
        }
    }

    public event Action<int> OnScoreChanged;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
    }

    private void Start()
    {
        // 初始：未开始
        HasStarted = false;
        IsPaused = false;
        IsGameOver = false;

        SetScore(0);

        UIManager.I?.ShowStart();
        UIManager.I?.SetScore(0);
    }

    private void Update()
    {
        // 暂停键：只有开始后且未结束才允许
        if (HasStarted && !IsGameOver && Input.GetKeyDown(KeyCode.P))
        {
            SetPaused(!IsPaused);
        }

        if (IsGameOver && Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }
    }

    public void StartGame()
    {
        HasStarted = true;
        IsPaused = false;
        IsGameOver = false;

        UIManager.I?.ShowHUD();
        UIManager.I?.ShowPause(false);
        UIManager.I?.ShowGameOver(false);
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        UIManager.I?.ShowPause(paused);
    }

    public bool InBounds(Vector2Int p) => p.x >= 0 && p.x < width && p.y >= 0 && p.y < height;
    public Vector3 GridToWorld(Vector2Int p)
    {
        // width=30 => x=0 -> -14.5, x=29 -> +14.5（中心在 0）
        float x = (p.x - (width - 1) * 0.5f) * cellSize + worldOrigin.x;
        float y = (p.y - (height - 1) * 0.5f) * cellSize + worldOrigin.y;
        return new Vector3(x, y, 0f);
    }

    public void AddScore(int delta)
    {
        SetScore(Score + delta);
    }

    private void SetScore(int s)
    {
        Score = Mathf.Max(0, s);
        UIManager.I?.SetScore(Score);
        OnScoreChanged?.Invoke(Score);
    }

    public void GameOver()
    {
        IsGameOver = true;
        UIManager.I?.ShowGameOver(true);
        AudioManager.I?.PlayDeath();
    }
}