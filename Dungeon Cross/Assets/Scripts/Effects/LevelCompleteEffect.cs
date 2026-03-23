using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelCompleteEffect : MonoBehaviour
{
    private const int ParticleCount = 8;
    private const float ParticleSize = 0.3f;
    private const float Speed = 3f;
    private const float Duration = 0.5f;

    private static Sprite cachedSprite;

    private readonly List<Transform> particleTransforms = new List<Transform>();
    private readonly List<SpriteRenderer> particleRenderers = new List<SpriteRenderer>();
    private readonly List<Vector3> particleDirections = new List<Vector3>();

    public void Play(Vector3 position)
    {
        transform.position = position;
        CreateParticles();
        StartCoroutine(AnimateParticles());
    }

    private void CreateParticles()
    {
        for (int index = 0; index < ParticleCount; index++)
        {
            float angle = (360f / ParticleCount) * index * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f).normalized;

            GameObject particleObject = new GameObject($"LevelCompleteParticle_{index}");
            particleObject.transform.SetParent(transform, false);
            particleObject.transform.localPosition = Vector3.zero;
            particleObject.transform.localScale = new Vector3(ParticleSize, ParticleSize, 1f);

            SpriteRenderer spriteRenderer = particleObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetOrCreateSprite();
            spriteRenderer.color = Color.yellow;
            spriteRenderer.sortingOrder = 20;

            particleTransforms.Add(particleObject.transform);
            particleRenderers.Add(spriteRenderer);
            particleDirections.Add(direction);
        }
    }

    private IEnumerator AnimateParticles()
    {
        float elapsed = 0f;

        while (elapsed < Duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / Duration);

            for (int index = 0; index < particleTransforms.Count; index++)
            {
                if (particleTransforms[index] != null)
                {
                    particleTransforms[index].position += particleDirections[index] * (Speed * Time.unscaledDeltaTime);
                }

                if (particleRenderers[index] != null)
                {
                    Color color = particleRenderers[index].color;
                    color.a = Mathf.Lerp(1f, 0f, normalizedTime);
                    particleRenderers[index].color = color;
                }
            }

            yield return null;
        }

        Destroy(gameObject);
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
