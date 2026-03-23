using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    private static Sprite cachedSprite;

    private void Start()
    {
        BuildKnightToken();
    }

    private void BuildKnightToken()
    {
        CreateLayer("Shadow", new Vector3(0.62f, 0.28f, 1f), new Color(0f, 0f, 0f, 0.22f), 3, new Vector3(0f, -0.28f, 0f));
        CreateLayer("Cloak", new Vector3(0.38f, 0.58f, 1f), new Color(0.64f, 0.14f, 0.18f, 1f), 4, new Vector3(0f, -0.06f, 0f), -8f);
        CreateLayer("Body", new Vector3(0.42f, 0.5f, 1f), new Color(0.82f, 0.84f, 0.9f, 1f), 5, new Vector3(0f, 0.02f, 0f));
        CreateLayer("Helmet", new Vector3(0.34f, 0.28f, 1f), new Color(0.94f, 0.96f, 1f, 1f), 6, new Vector3(0f, 0.27f, 0f));
        CreateLayer("Visor", new Vector3(0.16f, 0.06f, 1f), new Color(0.12f, 0.16f, 0.22f, 1f), 7, new Vector3(0f, 0.25f, 0f));
        CreateLayer("Shield", new Vector3(0.18f, 0.28f, 1f), new Color(0.2f, 0.46f, 0.72f, 1f), 6, new Vector3(-0.24f, 0.05f, 0f), 14f);
        CreateLayer("ShieldTrim", new Vector3(0.12f, 0.22f, 1f), new Color(0.9f, 0.94f, 1f, 0.45f), 7, new Vector3(-0.24f, 0.05f, 0f), 14f);
    }

    private void CreateLayer(string objectName, Vector3 scale, Color color, int sortingOrder, Vector3 localPosition, float rotation = 0f)
    {
        GameObject layer = new GameObject(objectName);
        layer.transform.SetParent(transform, false);
        layer.transform.localPosition = localPosition;
        layer.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        layer.transform.localScale = scale;

        SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
        renderer.sprite = GetOrCreateSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private static Sprite GetOrCreateSprite()
    {
        if (cachedSprite != null)
        {
            return cachedSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        cachedSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return cachedSprite;
    }
}
