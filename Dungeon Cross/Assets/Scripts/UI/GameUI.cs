using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    private Text hpText;
    private Text streakText;
    private Text centerText;
    private Text actionText;
    private bool waitingForRestart;
    private bool waitingForContinue;

    private void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        if (GetComponent<CanvasScaler>() == null)
        {
            gameObject.AddComponent<CanvasScaler>();
        }

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        CreateUiElements();
    }

    private void OnEnable()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnGameOver += HandleGameOver;
        GameManager.Instance.OnLevelComplete += HandleLevelComplete;
    }

    private void Start()
    {
        RefreshHp();
        ShowCenterMessage(string.Empty, string.Empty);
    }

    private void Update()
    {
        RefreshHp();
        RefreshStreak();

        if (!waitingForRestart && !waitingForContinue)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleTapAction();
            return;
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            HandleTapAction();
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnGameOver -= HandleGameOver;
        GameManager.Instance.OnLevelComplete -= HandleLevelComplete;
    }

    private void CreateUiElements()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        hpText = CreateText("HpText", font, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -10f), TextAnchor.UpperLeft, 32);
        streakText = CreateText("StreakText", font, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -50f), TextAnchor.UpperLeft, 28);
        centerText = CreateText("CenterText", font, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), TextAnchor.MiddleCenter, 48);
        actionText = CreateText("ActionText", font, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), TextAnchor.MiddleCenter, 28);
    }

    private Text CreateText(string objectName, Font font, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, TextAnchor alignment, int fontSize)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(transform, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(600f, 120f);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private void RefreshHp()
    {
        if (hpText == null)
        {
            return;
        }

        int hp = GameManager.Instance != null ? GameManager.Instance.CurrentHp : 0;
        hpText.text = $"HP: {hp}";
    }

    private void RefreshStreak()
    {
        if (streakText == null)
        {
            return;
        }

        int streak = GameManager.Instance != null ? GameManager.Instance.StreakCount : 0;
        streakText.text = $"Streak: {streak}";
    }

    private void HandleGameOver()
    {
        waitingForRestart = true;
        waitingForContinue = false;
        ShowCenterMessage("GAME OVER", "Tap to restart");
    }

    private void HandleLevelComplete()
    {
        waitingForContinue = true;
        waitingForRestart = false;
        ShowCenterMessage("LEVEL COMPLETE!", "Tap to continue");
    }

    private void ShowCenterMessage(string mainMessage, string subMessage)
    {
        if (centerText != null)
        {
            centerText.text = mainMessage;
        }

        if (actionText != null)
        {
            actionText.text = subMessage;
        }
    }

    private void HandleTapAction()
    {
        if (waitingForContinue)
        {
            waitingForContinue = false;
            ShowCenterMessage(string.Empty, string.Empty);

            GameManager.Instance?.Respawn();

            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                player.RespawnToStart();
            }

            LevelManager.Instance?.SpawnLevel(GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1);
            return;
        }

        if (waitingForRestart)
        {
            waitingForRestart = false;
            GameManager.Instance?.RestartGame();
        }
    }
}
