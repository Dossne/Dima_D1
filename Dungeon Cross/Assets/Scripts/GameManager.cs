using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action OnGameOver;
    public event Action OnLevelComplete;

    [SerializeField] private int startingHp = 3;
    [SerializeField] private int currentLevel = 1;

    public int CurrentHp { get; private set; }
    public int CurrentLevel => currentLevel;
    public int StreakCount { get; private set; }
    public int BestStreak { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsLevelComplete { get; private set; }
    public bool IsPaused { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CurrentHp = Mathf.Max(0, startingHp);
        IsGameOver = CurrentHp <= 0;
        BestStreak = PlayerPrefs.GetInt("BestStreak", 0);
    }

    public void TakeDamage(int amount = 1)
    {
        if (IsGameOver)
        {
            return;
        }

        CurrentHp = Mathf.Max(0, CurrentHp - Mathf.Max(0, amount));

        if (CurrentHp == 0)
        {
            UpdateBestStreak();
            IsGameOver = true;
            StreakCount = 0;
            IsPaused = false;
            Time.timeScale = 0f;
            OnGameOver?.Invoke();
        }
    }

    public void CheckWin(Vector2Int playerGridPosition)
    {
        if (IsGameOver || IsLevelComplete)
        {
            return;
        }

        if (playerGridPosition == new Vector2Int(4, 11))
        {
            IsLevelComplete = true;
            StreakCount++;
            UpdateBestStreak();
            IsPaused = false;
            Time.timeScale = 0f;
            OnLevelComplete?.Invoke();
        }
    }

    public void Respawn()
    {
        IsGameOver = false;
        IsLevelComplete = false;
        IsPaused = false;
        currentLevel++;
        Time.timeScale = 1f;
    }

    public void RestartGame()
    {
        CurrentHp = Mathf.Max(0, startingHp);
        currentLevel = 1;
        StreakCount = 0;
        IsGameOver = false;
        IsLevelComplete = false;
        IsPaused = false;
        Time.timeScale = 1f;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
            return;
        }

        SceneManager.LoadScene(activeScene.name);
    }

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }

    public void ResetGame()
    {
        CurrentHp = Mathf.Max(0, startingHp);
        currentLevel = 1;
        IsGameOver = CurrentHp <= 0;
        IsLevelComplete = false;
        StreakCount = 0;
        IsPaused = false;
        Time.timeScale = 1f;
    }

    private void UpdateBestStreak()
    {
        if (StreakCount <= BestStreak)
        {
            return;
        }

        BestStreak = StreakCount;
        PlayerPrefs.SetInt("BestStreak", BestStreak);
        PlayerPrefs.Save();
    }
}
