using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    private Text hpText;
    private Text streakText;
    private Text bestText;
    private Text centerText;
    private Text actionText;
    private RectTransform overlayPanelRect;
    private RectTransform pauseButtonRect;
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
            CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
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
        RefreshStreak();
        RefreshBest();
        ShowCenterMessage(string.Empty, string.Empty);
    }

    private void Update()
    {
        RefreshHp();
        RefreshStreak();
        RefreshBest();

        if (Input.GetKeyDown(KeyCode.Escape) && !waitingForRestart && !waitingForContinue && PauseMenu.Instance != null)
        {
            TogglePauseMenu();
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Vector2 touchPosition = Input.GetTouch(0).position;

            if (!waitingForRestart && !waitingForContinue && HandlePauseButtonTap(touchPosition))
            {
                return;
            }

            if (waitingForRestart || waitingForContinue)
            {
                HandleTapAction();
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            if (!waitingForRestart && !waitingForContinue && HandlePauseButtonTap(Input.mousePosition))
            {
                return;
            }

            if (waitingForRestart || waitingForContinue)
            {
                HandleTapAction();
            }
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
        bestText = CreateText("BestText", font, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -90f), TextAnchor.UpperLeft, 28);
        CreatePauseButton(font);

        GameObject overlayPanel = new GameObject("OverlayPanel");
        overlayPanel.transform.SetParent(transform, false);
        overlayPanelRect = overlayPanel.AddComponent<RectTransform>();
        overlayPanelRect.anchorMin = Vector2.zero;
        overlayPanelRect.anchorMax = Vector2.one;
        overlayPanelRect.offsetMin = Vector2.zero;
        overlayPanelRect.offsetMax = Vector2.zero;

        centerText = CreateOverlayText("CenterText", font, new Vector2(0f, 80f), 48);
        actionText = CreateOverlayText("ActionText", font, new Vector2(0f, -20f), 28);
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

    private Text CreateOverlayText(string objectName, Font font, Vector2 anchoredPosition, int fontSize)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(overlayPanelRect, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(40f, 0f);
        rectTransform.offsetMax = new Vector2(-40f, 0f);
        rectTransform.anchoredPosition = anchoredPosition;

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
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

    private void RefreshBest()
    {
        if (bestText == null)
        {
            return;
        }

        int best = GameManager.Instance != null ? GameManager.Instance.BestStreak : 0;
        bestText.text = $"BEST: {best}";
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

        GameObject effectObject = new GameObject("LevelCompleteEffect");
        LevelCompleteEffect effect = effectObject.AddComponent<LevelCompleteEffect>();
        effect.Play(Vector3.zero);
    }

    private void ShowCenterMessage(string mainMessage, string subMessage)
    {
        if (centerText != null)
        {
            if (mainMessage == "LEVEL COMPLETE!")
            {
                centerText.fontSize = 60;
            }
            else if (mainMessage == "GAME OVER")
            {
                centerText.fontSize = 70;
            }
            else
            {
                centerText.fontSize = 48;
            }

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

    private void CreatePauseButton(Font font)
    {
        GameObject buttonObject = new GameObject("PauseButton");
        buttonObject.transform.SetParent(transform, false);

        pauseButtonRect = buttonObject.AddComponent<RectTransform>();
        pauseButtonRect.anchorMin = new Vector2(1f, 1f);
        pauseButtonRect.anchorMax = new Vector2(1f, 1f);
        pauseButtonRect.pivot = new Vector2(1f, 1f);
        pauseButtonRect.anchoredPosition = new Vector2(-20f, -20f);
        pauseButtonRect.sizeDelta = new Vector2(80f, 80f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(2f, 2f);

        GameObject textObject = new GameObject("PauseButtonText");
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = 36;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = "II";
    }

    private bool HandlePauseButtonTap(Vector2 screenPosition)
    {
        if (pauseButtonRect == null || GameManager.Instance == null || PauseMenu.Instance == null)
        {
            return false;
        }

        if (!RectTransformUtility.RectangleContainsScreenPoint(pauseButtonRect, screenPosition, null))
        {
            return false;
        }

        TogglePauseMenu();
        return true;
    }

    private void TogglePauseMenu()
    {
        if (GameManager.Instance == null || PauseMenu.Instance == null)
        {
            return;
        }

        if (GameManager.Instance.IsPaused)
        {
            GameManager.Instance.Resume();
            PauseMenu.Instance.Hide();
        }
        else
        {
            GameManager.Instance.Pause();
            PauseMenu.Instance.Show();
        }
    }
}
