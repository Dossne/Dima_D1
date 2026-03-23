using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    private Canvas canvas;
    private Text soundText;
    private Text bestText;
    private Text musicLabelText;
    private Text musicValueText;
    private bool soundEnabled;
    private RectTransform safeAreaRect;
    private Rect lastSafeArea;

    private RectTransform resumeButtonRect;
    private RectTransform restartButtonRect;
    private RectTransform soundButtonRect;
    private RectTransform musicSliderTrackRect;
    private RectTransform musicSliderFillRect;
    private RectTransform musicSliderHandleRect;
    private bool draggingMusicSlider;

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
            draggingMusicSlider = false;
            return;
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (HandleMusicSliderTouch(touch))
            {
                return;
            }

            if (touch.phase == TouchPhase.Began)
            {
                HandleButtonTap(touch.position);
            }
        }
        else
        {
            HandleMusicSliderMouse();

            if (Input.GetMouseButtonDown(0) && !draggingMusicSlider)
            {
                HandleButtonTap(Input.mousePosition);
            }
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
        RefreshMusicSlider();
    }

    public void Hide()
    {
        draggingMusicSlider = false;

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

        float startY = -400f;
        CreateButton(font, "ResumeButton", "RESUME", new Vector2(0f, startY), out resumeButtonRect);
        CreateButton(font, "RestartButton", "RESTART", new Vector2(0f, startY - 136f), out restartButtonRect);
        CreateButton(font, "SoundButton", soundEnabled ? "SOUND: ON" : "SOUND: OFF", new Vector2(0f, startY - 272f), out soundButtonRect, out soundText);
        CreateMusicSlider(font, new Vector2(0f, startY - 410f));

        bestText = CreateText("BestText", font, 44, FontStyle.Normal, new Color(1f, 215f / 255f, 0f, 1f));
        bestText.transform.SetParent(safeAreaRect, false);
        RectTransform bestRect = bestText.rectTransform;
        bestRect.anchorMin = new Vector2(0.5f, 0.5f);
        bestRect.anchorMax = new Vector2(0.5f, 0.5f);
        bestRect.anchoredPosition = new Vector2(0f, startY - 560f);
        bestRect.sizeDelta = new Vector2(860f, 84f);

        RefreshTexts();
        RefreshMusicSlider();
    }

    private void CreateMusicSlider(Font font, Vector2 anchoredPosition)
    {
        musicLabelText = CreateText("MusicLabel", font, 34, FontStyle.Normal, Color.white);
        musicLabelText.transform.SetParent(safeAreaRect, false);
        musicLabelText.text = "MUSIC VOLUME";

        RectTransform labelRect = musicLabelText.rectTransform;
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = anchoredPosition + new Vector2(0f, 0f);
        labelRect.sizeDelta = new Vector2(520f, 48f);

        GameObject sliderObject = new GameObject("MusicSliderTrack");
        sliderObject.transform.SetParent(safeAreaRect, false);
        musicSliderTrackRect = sliderObject.AddComponent<RectTransform>();
        musicSliderTrackRect.anchorMin = new Vector2(0.5f, 0.5f);
        musicSliderTrackRect.anchorMax = new Vector2(0.5f, 0.5f);
        musicSliderTrackRect.anchoredPosition = anchoredPosition + new Vector2(0f, -56f);
        musicSliderTrackRect.sizeDelta = new Vector2(560f, 36f);

        Image trackImage = sliderObject.AddComponent<Image>();
        trackImage.color = new Color(1f, 1f, 1f, 0.12f);

        Outline trackOutline = sliderObject.AddComponent<Outline>();
        trackOutline.effectColor = new Color(1f, 1f, 1f, 0.8f);
        trackOutline.effectDistance = new Vector2(2f, 2f);

        GameObject fillObject = new GameObject("MusicSliderFill");
        fillObject.transform.SetParent(sliderObject.transform, false);
        musicSliderFillRect = fillObject.AddComponent<RectTransform>();
        musicSliderFillRect.anchorMin = new Vector2(0f, 0f);
        musicSliderFillRect.anchorMax = new Vector2(0f, 1f);
        musicSliderFillRect.pivot = new Vector2(0f, 0.5f);
        musicSliderFillRect.offsetMin = new Vector2(0f, 0f);
        musicSliderFillRect.offsetMax = new Vector2(0f, 0f);

        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.color = new Color(0.3f, 0.85f, 1f, 0.9f);

        GameObject handleObject = new GameObject("MusicSliderHandle");
        handleObject.transform.SetParent(sliderObject.transform, false);
        musicSliderHandleRect = handleObject.AddComponent<RectTransform>();
        musicSliderHandleRect.anchorMin = new Vector2(0f, 0.5f);
        musicSliderHandleRect.anchorMax = new Vector2(0f, 0.5f);
        musicSliderHandleRect.pivot = new Vector2(0.5f, 0.5f);
        musicSliderHandleRect.sizeDelta = new Vector2(44f, 52f);

        Image handleImage = handleObject.AddComponent<Image>();
        handleImage.color = Color.white;

        Outline handleOutline = handleObject.AddComponent<Outline>();
        handleOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        handleOutline.effectDistance = new Vector2(2f, 2f);

        musicValueText = CreateText("MusicValue", font, 30, FontStyle.Normal, new Color(0.85f, 0.95f, 1f, 1f));
        musicValueText.transform.SetParent(safeAreaRect, false);
        RectTransform valueRect = musicValueText.rectTransform;
        valueRect.anchorMin = new Vector2(0.5f, 0.5f);
        valueRect.anchorMax = new Vector2(0.5f, 0.5f);
        valueRect.anchoredPosition = anchoredPosition + new Vector2(0f, -112f);
        valueRect.sizeDelta = new Vector2(520f, 40f);
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

    private bool HandleMusicSliderTouch(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                if (TryBeginMusicSliderDrag(touch.position))
                {
                    return true;
                }
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (draggingMusicSlider)
                {
                    UpdateMusicSliderFromScreenPosition(touch.position);
                    return true;
                }
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                draggingMusicSlider = false;
                break;
        }

        return false;
    }

    private void HandleMusicSliderMouse()
    {
        if (Input.GetMouseButtonDown(0) && TryBeginMusicSliderDrag(Input.mousePosition))
        {
            return;
        }

        if (draggingMusicSlider && Input.GetMouseButton(0))
        {
            UpdateMusicSliderFromScreenPosition(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            draggingMusicSlider = false;
        }
    }

    private bool TryBeginMusicSliderDrag(Vector2 screenPosition)
    {
        if (musicSliderTrackRect == null)
        {
            return false;
        }

        if (!RectTransformUtility.RectangleContainsScreenPoint(musicSliderTrackRect, screenPosition, null)
            && (musicSliderHandleRect == null || !RectTransformUtility.RectangleContainsScreenPoint(musicSliderHandleRect, screenPosition, null)))
        {
            return false;
        }

        draggingMusicSlider = true;
        UpdateMusicSliderFromScreenPosition(screenPosition);
        return true;
    }

    private void UpdateMusicSliderFromScreenPosition(Vector2 screenPosition)
    {
        if (musicSliderTrackRect == null)
        {
            return;
        }

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(musicSliderTrackRect, screenPosition, null, out localPoint))
        {
            return;
        }

        float normalized = Mathf.InverseLerp(-musicSliderTrackRect.rect.width * 0.5f, musicSliderTrackRect.rect.width * 0.5f, localPoint.x);
        AudioManager.Instance?.SetMusicVolume(normalized);
        RefreshMusicSlider();
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

    private void RefreshMusicSlider()
    {
        if (musicSliderTrackRect == null || musicSliderFillRect == null || musicSliderHandleRect == null)
        {
            return;
        }

        float musicVolume = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.5f;
        float width = musicSliderTrackRect.rect.width;
        float clampedWidth = Mathf.Clamp(width * musicVolume, 0f, width);

        musicSliderFillRect.sizeDelta = new Vector2(clampedWidth, 0f);
        musicSliderHandleRect.anchoredPosition = new Vector2((width * musicVolume) - width * 0.5f, 0f);

        if (musicValueText != null)
        {
            musicValueText.text = $"{Mathf.RoundToInt(musicVolume * 100f)}%";
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

        RefreshMusicSlider();
    }
}
