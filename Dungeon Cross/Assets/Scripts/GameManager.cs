using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private int startingHp = 3;
    [SerializeField] private int currentLevel = 1;

    public int CurrentHp { get; private set; }
    public int CurrentLevel => currentLevel;
    public bool IsGameOver { get; private set; }

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
        }
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
    }
}
