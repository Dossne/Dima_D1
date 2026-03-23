using System.Reflection;
using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    private static readonly FieldInfo CurrentGridPositionField = typeof(PlayerController).GetField("currentGridPosition", BindingFlags.Instance | BindingFlags.NonPublic);

    private SpriteRenderer spriteRenderer;
    private PlayerController playerController;
    private static Sprite cachedSprite;

    private void Start()
    {
        playerController = GetComponent<PlayerController>();

        GameObject visualObject = new GameObject("PlayerVisualSprite");
        visualObject.transform.SetParent(transform, false);
        spriteRenderer = visualObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetOrCreateSprite();
        spriteRenderer.color = Color.white;
    }

    private void Update()
    {
        if (playerController == null || GridManager.Instance == null)
        {
            return;
        }

        if (CurrentGridPositionField?.GetValue(playerController) is Vector2Int gridPosition)
        {
            transform.position = GridManager.Instance.GridToWorld(gridPosition);
        }
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
