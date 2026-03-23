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
    public bool IsGameOver { get; private set; }
    public bool IsLevelComplete { get; private set; }

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
            IsGameOver = true;
            StreakCount = 0;
            Time.timeScale = 0f;
            OnGameOver?.Invoke();
        }
    }

    public void CheckWin(int playerRow)
    {
        if (IsGameOver || IsLevelComplete)
        {
            return;
        }

        if (playerRow >= 8)
        {
            IsLevelComplete = true;
            StreakCount++;
            Time.timeScale = 0f;
            OnLevelComplete?.Invoke();
        }
    }

    public void Respawn()
    {
        CurrentHp = Mathf.Max(0, startingHp);
        IsGameOver = false;
        IsLevelComplete = false;
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

    public void AdvanceLevel()
    {
        currentLevel++;
    }

    public void ResetGame()
    {
        CurrentHp = Mathf.Max(0, startingHp);
        currentLevel = 1;
        IsGameOver = CurrentHp <= 0;
        IsLevelComplete = false;
        StreakCount = 0;
        Time.timeScale = 1f;
    }
}
