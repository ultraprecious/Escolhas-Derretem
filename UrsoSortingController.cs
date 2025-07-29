using UnityEngine;

public class UrsoSortingController : MonoBehaviour
{
    [Tooltip("Nome da Sorting Layer a ser usada quando o urso estiver 'submerso' na água.")]
    [SerializeField] private string sortingLayerNameUrsoSubmerso = "UrsoSubmerso";
    [Tooltip("Order in Layer (dentro da Sorting Layer 'UrsoSubmerso') quando o urso estiver submerso.")]
    [SerializeField] private int orderInLayerUrsoSubmerso = 0;

    private SpriteRenderer spriteRenderer;
    private int originalSortingLayerID;
    private int originalOrderInLayer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("UrsoSortingController requer um SpriteRenderer no GameObject.", this);
            enabled = false; // Desativa o script se não houver SpriteRenderer
            return;
        }

        // Armazenar o estado original da Sorting Layer no Awake
        originalSortingLayerID = spriteRenderer.sortingLayerID;
        originalOrderInLayer = spriteRenderer.sortingOrder;
    }

    // Método público para controlar a Sorting Layer do urso
    public void SetBearSubmergedSorting(bool submerged)
    {
        if (spriteRenderer == null) return;

        if (submerged)
        {
            // Encontra o SortingLayerID da nova camada
            int submergedLayerID = SortingLayer.NameToID(sortingLayerNameUrsoSubmerso);
            if (submergedLayerID == 0 && sortingLayerNameUrsoSubmerso != "Default")
            {
                Debug.LogWarning($"Sorting Layer '{sortingLayerNameUrsoSubmerso}' não encontrada. Verifique se ela foi criada nas Project Settings > Tags and Layers.");
                // Fallback: Se a camada não existir, apenas tenta definir uma Order in Layer bem baixa na camada atual
                spriteRenderer.sortingOrder = -100; // Um valor bem baixo para tentar ir para trás
                return;
            }
            spriteRenderer.sortingLayerID = submergedLayerID;
            spriteRenderer.sortingOrder = orderInLayerUrsoSubmerso;
            Debug.Log($"[UrsoSortingController] Urso setado para Submerso: Layer='{sortingLayerNameUrsoSubmerso}', Order={orderInLayerUrsoSubmerso}");
        }
        else // Não submerso, restaurar original
        {
            spriteRenderer.sortingLayerID = originalSortingLayerID;
            spriteRenderer.sortingOrder = originalOrderInLayer;
            Debug.Log($"[UrsoSortingController] Urso restaurado para Original: Layer='{SortingLayer.IDToName(originalSortingLayerID)}', Order={originalOrderInLayer}");
        }
    }
}