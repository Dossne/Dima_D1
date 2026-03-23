using UnityEngine;
using UnityEngine.UI;

public class StartScreen : MonoBehaviour
{
    private Text tapText;
    private bool hasStarted;

    private void Awake()
    {
        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        if (gameObject.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        CreateUi();
    }

    private void Update()
    {
        PulseTapText();

        if (hasStarted)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            BeginGame();
            return;
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            BeginGame();
        }
    }

    private void CreateUi()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Image background = CreateImage("Background", Color.black);
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        background.color = new Color(0f, 0f, 0f, 0.85f);

        Text titleText = CreateText("TitleText", font, 56, TextAnchor.MiddleCenter, Color.white);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 110f);
        titleRect.sizeDelta = new Vector2(900f, 120f);
        titleText.text = "DUNGEON CROSS";

        Text flavorText = CreateText("FlavorText", font, 24, TextAnchor.MiddleCenter, new Color(0.75f, 0.75f, 0.75f, 1f));
        RectTransform flavorRect = flavorText.rectTransform;
        flavorRect.anchorMin = new Vector2(0.5f, 0.5f);
        flavorRect.anchorMax = new Vector2(0.5f, 0.5f);
        flavorRect.anchoredPosition = new Vector2(0f, 20f);
        flavorRect.sizeDelta = new Vector2(900f, 120f);
        flavorText.text = "A knight without a sword.\nOnly wit can carry you through.";

        tapText = CreateText("TapText", font, 30, TextAnchor.MiddleCenter, Color.white);
        RectTransform tapRect = tapText.rectTransform;
        tapRect.anchorMin = new Vector2(0.5f, 0f);
        tapRect.anchorMax = new Vector2(0.5f, 0f);
        tapRect.anchoredPosition = new Vector2(0f, 80f);
        tapRect.sizeDelta = new Vector2(900f, 80f);
        tapText.text = "TAP TO BEGIN";
    }

    private void PulseTapText()
    {
        if (tapText == null)
        {
            return;
        }

        float alpha = Mathf.Lerp(0.5f, 1f, (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f) + 1f) * 0.5f);
        Color color = tapText.color;
        color.a = alpha;
        tapText.color = color;
    }

    private void BeginGame()
    {
        hasStarted = true;
        PlayerController.GameStarted = true;

        GameManager.Instance?.ResetGame();
        PlayerController.Instance?.RespawnToStart();
        LevelManager.Instance?.SpawnLevel(1);
        Destroy(gameObject);
    }

    private Image CreateImage(string objectName, Color color)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(transform, false);

        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private Text CreateText(string objectName, Font font, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(transform, false);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
