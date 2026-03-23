using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    private Canvas canvas;
    private Text soundText;
    private Text bestText;
    private bool soundEnabled;
    private RectTransform safeAreaRect;
    private Rect lastSafeArea;

    private RectTransform resumeButtonRect;
    private RectTransform restartButtonRect;
    private RectTransform soundButtonRect;

    public bool IsVisible => canvas != null && canvas.enabled;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        canvas.enabled = false;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        soundEnabled = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
        BuildUi();
        RefreshSafeArea();
        AudioManager.Instance?.RefreshFromPrefs();
    }

    private void Update()
    {
        RefreshSafeArea();

        if (!IsVisible)
        {
            return;
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            HandleButtonTap(Input.GetTouch(0).position);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            HandleButtonTap(Input.mousePosition);
        }
    }

    public void Show()
    {
        if (canvas == null)
        {
            return;
        }

        canvas.enabled = true;
        RefreshTexts();
    }

    public void Hide()
    {
        if (canvas != null)
        {
            canvas.enabled = false;
        }
    }

    private void BuildUi()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject overlayObject = new GameObject("PauseOverlay");
        overlayObject.transform.SetParent(transform, false);
        Image overlay = overlayObject.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.92f);

        RectTransform overlayRect = overlay.rectTransform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        safeAreaRect = CreateSafeAreaRoot();

        Text titleText = CreateText("PauseTitle", font, 78, FontStyle.Bold, Color.white);
        titleText.transform.SetParent(safeAreaRect, false);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -140f);
        titleRect.sizeDelta = new Vector2(880f, 130f);
        titleText.text = "PAUSED";

        float startY = -430f;
        CreateButton(font, "ResumeButton", "RESUME", new Vector2(0f, startY), out resumeButtonRect);
        CreateButton(font, "RestartButton", "RESTART", new Vector2(0f, startY - 136f), out restartButtonRect);
        CreateButton(font, "SoundButton", soundEnabled ? "SOUND: ON" : "SOUND: OFF", new Vector2(0f, startY - 272f), out soundButtonRect, out soundText);

        bestText = CreateText("BestText", font, 44, FontStyle.Normal, new Color(1f, 215f / 255f, 0f, 1f));
        bestText.transform.SetParent(safeAreaRect, false);
        RectTransform bestRect = bestText.rectTransform;
        bestRect.anchorMin = new Vector2(0.5f, 0.5f);
        bestRect.anchorMax = new Vector2(0.5f, 0.5f);
        bestRect.anchoredPosition = new Vector2(0f, startY - 430f);
        bestRect.sizeDelta = new Vector2(860f, 84f);

        RefreshTexts();
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

    private void CreateButton(Font font, string objectName, string label, Vector2 anchoredPosition, out RectTransform buttonRect)
    {
        CreateButton(font, objectName, label, anchoredPosition, out buttonRect, out _);
    }

    private void CreateButton(Font font, string objectName, string label, Vector2 anchoredPosition, out RectTransform buttonRect, out Text labelText)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(safeAreaRect, false);

        buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(560f, 108f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(3f, 3f);

        labelText = CreateText($"{objectName}Text", font, 50, FontStyle.Normal, Color.white);
        labelText.transform.SetParent(buttonObject.transform, false);
        labelText.text = label;

        RectTransform textRect = labelText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 0f);
        textRect.offsetMax = new Vector2(-20f, 0f);
    }

    private Text CreateText(string objectName, Font font, int fontSize, FontStyle fontStyle, Color color)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(transform, false);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private void HandleButtonTap(Vector2 screenPosition)
    {
        if (resumeButtonRect != null && RectTransformUtility.RectangleContainsScreenPoint(resumeButtonRect, screenPosition, null))
        {
            GameManager.Instance.Resume();
            canvas.enabled = false;
            return;
        }

        if (restartButtonRect != null && RectTransformUtility.RectangleContainsScreenPoint(restartButtonRect, screenPosition, null))
        {
            GameManager.Instance.Resume();
            GameManager.Instance.ReloadGame();
            canvas.enabled = false;
            return;
        }

        if (soundButtonRect != null && RectTransformUtility.RectangleContainsScreenPoint(soundButtonRect, screenPosition, null))
        {
            soundEnabled = !soundEnabled;
            PlayerPrefs.SetInt("SoundEnabled", soundEnabled ? 1 : 0);
            PlayerPrefs.Save();
            AudioManager.Instance?.SetSoundEnabled(soundEnabled);

            if (soundText != null)
            {
                soundText.text = soundEnabled ? "SOUND: ON" : "SOUND: OFF";
            }
        }
    }

    private void RefreshTexts()
    {
        if (soundText != null)
        {
            soundText.text = soundEnabled ? "SOUND: ON" : "SOUND: OFF";
        }

        if (bestText != null)
        {
            int best = GameManager.Instance != null ? GameManager.Instance.BestStreak : PlayerPrefs.GetInt("BestStreak", 0);
            bestText.text = $"BEST: {best}";
        }
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
}
