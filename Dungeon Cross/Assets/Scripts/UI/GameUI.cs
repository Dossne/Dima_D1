using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    private Text hpText;
    private Text streakText;
    private Text bestText;
    private Text centerText;
    private Text actionText;
    private RectTransform safeAreaRect;
    private RectTransform overlayPanelRect;
    private RectTransform pauseButtonRect;
    private bool waitingForRestart;
    private bool waitingForContinue;
    private Rect lastSafeArea;

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
        RefreshSafeArea();
    }

    private void OnEnable()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnGameOver += HandleGameOver;
        GameManager.Instance.OnLevelComplete += HandleLevelComplete;
        GameManager.Instance.OnRunComplete += HandleRunComplete;
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
        RefreshSafeArea();
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
        GameManager.Instance.OnRunComplete -= HandleRunComplete;
    }

    private void CreateUiElements()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        safeAreaRect = CreateSafeAreaRoot();

        hpText = CreateText("HpText", font, safeAreaRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -28f), TextAnchor.UpperLeft, 40, new Vector2(680f, 84f));
        streakText = CreateText("StreakText", font, safeAreaRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -84f), TextAnchor.UpperLeft, 34, new Vector2(680f, 72f));
        bestText = CreateText("BestText", font, safeAreaRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -136f), TextAnchor.UpperLeft, 34, new Vector2(680f, 72f));
        CreatePauseButton(font);

        GameObject overlayPanel = new GameObject("OverlayPanel");
        overlayPanel.transform.SetParent(safeAreaRect, false);
        overlayPanelRect = overlayPanel.AddComponent<RectTransform>();
        overlayPanelRect.anchorMin = Vector2.zero;
        overlayPanelRect.anchorMax = Vector2.one;
        overlayPanelRect.offsetMin = Vector2.zero;
        overlayPanelRect.offsetMax = Vector2.zero;

        centerText = CreateOverlayText("CenterText", font, new Vector2(0f, 88f), 60);
        actionText = CreateOverlayText("ActionText", font, new Vector2(0f, -28f), 34);
    }

    private RectTransform CreateSafeAreaRoot()
    {
        GameObject safeAreaObject = new GameObject("SafeAreaRoot");
        safeAreaObject.transform.SetParent(transform, false);

        RectTransform rectTransform = safeAreaObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        return rectTransform;
    }

    private Text CreateText(string objectName, Font font, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, TextAnchor alignment, int fontSize, Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

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
        rectTransform.offsetMin = new Vector2(72f, 0f);
        rectTransform.offsetMax = new Vector2(-72f, 0f);
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
        PlayLevelCompleteEffect();
    }

    private void HandleRunComplete()
    {
        waitingForRestart = true;
        waitingForContinue = false;
        ShowCenterMessage("VICTORY!", "Congratulations! Tap to restart");
        PlayLevelCompleteEffect();
    }

    private void PlayLevelCompleteEffect()
    {
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
                centerText.fontSize = 66;
            }
            else if (mainMessage == "GAME OVER")
            {
                centerText.fontSize = 72;
            }
            else if (mainMessage == "VICTORY!")
            {
                centerText.fontSize = 72;
            }
            else
            {
                centerText.fontSize = 60;
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

            GameManager.Instance?.AdvanceToNextLevel();

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
            ShowCenterMessage(string.Empty, string.Empty);
            GameManager.Instance?.ReloadGame();
        }
    }

    private void CreatePauseButton(Font font)
    {
        GameObject buttonObject = new GameObject("PauseButton");
        buttonObject.transform.SetParent(safeAreaRect, false);

        pauseButtonRect = buttonObject.AddComponent<RectTransform>();
        pauseButtonRect.anchorMin = new Vector2(1f, 1f);
        pauseButtonRect.anchorMax = new Vector2(1f, 1f);
        pauseButtonRect.pivot = new Vector2(1f, 1f);
        pauseButtonRect.anchoredPosition = new Vector2(-28f, -28f);
        pauseButtonRect.sizeDelta = new Vector2(96f, 96f);

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
        text.fontSize = 40;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = "II";
    }

    private void RefreshSafeArea()
    {
        if (safeAreaRect == null)
        {
            return;
        }

        Rect safeArea = Screen.safeArea;
        if (safeArea == lastSafeArea && safeArea.width > 0f && safeArea.height > 0f)
        {
            return;
        }

        lastSafeArea = safeArea;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        safeAreaRect.anchorMin = anchorMin;
        safeAreaRect.anchorMax = anchorMax;
        safeAreaRect.offsetMin = Vector2.zero;
        safeAreaRect.offsetMax = Vector2.zero;
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
