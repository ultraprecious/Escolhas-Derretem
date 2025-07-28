using UnityEngine;
using System.Collections; // Necessário para Coroutines

public class BanquisaController : MonoBehaviour
{
    [Header("Configurações da Banquisa")]
    [Tooltip("Duração do processo de derretimento (opacidade de 1 para 0).")]
    [SerializeField] public float meltDuration = 1.0f; // MUDADO: de private para PUBLIC
    [Tooltip("Duração do processo de restauração (opacidade de 0 para 1).")]
    [SerializeField] private float restoreDuration = 1.0f; // Tempo para a banquisa reaparecer
    [Tooltip("Offset Y para posicionar o urso corretamente ao teleportar para esta banquisa.")]
    [SerializeField] private float yOffsetTeleporteUrso = 0.5f;

    private SpriteRenderer spriteRenderer;
    private Collider2D banquisaCollider;
    private Color originalColor;

    private Coroutine currentFadeCoroutine;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        banquisaCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null) Debug.LogError("BanquisaController requer um SpriteRenderer no GameObject.", this);
        if (banquisaCollider == null) Debug.LogError("BanquisaController requer um Collider2D no GameObject.", this);

        originalColor = spriteRenderer.color;
        RestaurarBanquisaInstantaneamente();
    }

    public void DerreterBanquisa()
    {
        if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
        currentFadeCoroutine = StartCoroutine(FadeSprite(0f, meltDuration, true));
        Debug.Log($"Banquisa '{gameObject.name}' derretendo!");
    }

    public void RestaurarBanquisa()
    {
        if (banquisaCollider != null) banquisaCollider.enabled = true;

        if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
        currentFadeCoroutine = StartCoroutine(FadeSprite(originalColor.a, restoreDuration, false));
        Debug.Log($"Banquisa '{gameObject.name}' restaurada!");
    }

    private void RestaurarBanquisaInstantaneamente()
    {
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        if (banquisaCollider != null) banquisaCollider.enabled = true;
    }

    private IEnumerator FadeSprite(float targetAlpha, float duration, bool isMelting)
    {
        if (spriteRenderer == null) yield break;

        Color startColor = spriteRenderer.color;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            Color currentColor = startColor;
            currentColor.a = Mathf.Lerp(startColor.a, targetAlpha, progress);
            spriteRenderer.color = currentColor;
            yield return null;
        }

        Color finalColor = spriteRenderer.color;
        finalColor.a = targetAlpha;
        spriteRenderer.color = finalColor;

        if (isMelting && banquisaCollider != null)
        {
            banquisaCollider.enabled = false;
            Debug.Log($"Banquisa '{gameObject.name}' colisor desativado após derretimento visual.");
        }
    }

    public Vector3 GetPosicaoTeleporteUrso()
    {
        return new Vector3(transform.position.x, banquisaCollider.bounds.max.y + yOffsetTeleporteUrso, transform.position.z);
    }
}