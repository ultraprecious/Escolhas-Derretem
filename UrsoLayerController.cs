using UnityEngine;
using System.Collections; // Necessário para coroutines, se for usar fading de layer

public class UrsoLayerController : MonoBehaviour
{
    [Header("Configurações de Layers")]
    [Tooltip("O Sorting Layer para quando o urso estiver no gelo.")]
    [SerializeField] private string layerGeloSortingLayerName = "Personagem"; // Nome do seu Sorting Layer para personagens no gelo
    [Tooltip("A Ordem em Layer para quando o urso estiver no gelo (maior = mais acima).")]
    [SerializeField] private int layerGeloOrderInLayer = 0; // Ordem em Layer para o urso no gelo

    [Tooltip("O Sorting Layer para quando o urso estiver na água (para parecer submerso).")]
    [SerializeField] private string layerAguaSortingLayerName = "Agua"; // Nome do seu Sorting Layer para objetos submersos na água
    [Tooltip("A Ordem em Layer para quando o urso estiver na água (menor = mais abaixo).")]
    [SerializeField] private int layerAguaOrderInLayer = -1; // Ordem em Layer para o urso na água (abaixo do gelo/personagens)

    [Tooltip("O Layer de Física 2D para quando o urso estiver no gelo (para colisões).")]
    [SerializeField] private int physicsLayerGelo = 8; // Ex: Layer "Urso"
    [Tooltip("O Layer de Física 2D para quando o urso estiver na água (para evitar colisões com gelo se necessário).")]
    [SerializeField] private int physicsLayerAgua = 4; // Ex: Layer "UrsoAgua" ou "Default" se não houver colisões

    [Header("Componentes Necessários")]
    [SerializeField] private SpriteRenderer ursoSpriteRenderer;
    [SerializeField] private GameObject ursoGameObject; // Referência ao GameObject do urso para alterar o layer de física

    private MovimentoUrso movimentoUrso; // Para checar o estado de 'estaNaAgua'

    void Awake()
    {
        if (ursoSpriteRenderer == null)
        {
            ursoSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (ursoSpriteRenderer == null)
            {
                Debug.LogError("UrsoLayerController: SpriteRenderer do urso não atribuído e não encontrado nos filhos.", this);
                enabled = false;
                return;
            }
        }

        if (ursoGameObject == null)
        {
            ursoGameObject = this.gameObject; // Assume que este script está no GameObject raiz do urso
        }

        movimentoUrso = GetComponent<MovimentoUrso>();
        if (movimentoUrso == null)
        {
            Debug.LogError("UrsoLayerController: MovimentoUrso não encontrado no mesmo GameObject ou nos pais.", this);
            enabled = false;
            return;
        }

        // Garante que o urso comece na layer correta
        SetLayerUrsoNoGelo();
    }

    // Chamado para colocar o urso na layer de "gelo" (acima da água)
    public void SetLayerUrsoNoGelo()
    {
        if (ursoSpriteRenderer != null)
        {
            ursoSpriteRenderer.sortingLayerName = layerGeloSortingLayerName;
            ursoSpriteRenderer.sortingOrder = layerGeloOrderInLayer;
        }
        if (ursoGameObject != null)
        {
            ursoGameObject.layer = physicsLayerGelo;
        }
        Debug.Log("[UrsoLayerController] Urso está na Layer de Gelo (visualmente e fisicamente).");
    }

    // Chamado para colocar o urso na layer de "água" (submerso)
    public void SetLayerUrsoNaAgua()
    {
        if (ursoSpriteRenderer != null)
        {
            ursoSpriteRenderer.sortingLayerName = layerAguaSortingLayerName;
            ursoSpriteRenderer.sortingOrder = layerAguaOrderInLayer;
        }
        if (ursoGameObject != null)
        {
            ursoGameObject.layer = physicsLayerAgua;
        }
        Debug.Log("[UrsoLayerController] Urso está na Layer de Água (visualmente e fisicamente).");
    }

    // Método para ser chamado externamente (QuizManager ou MovimentoUrso)
    // Este método pode ser chamado quando o estado do urso muda.
    public void UpdateUrsoLayerBasedOnState()
    {
        if (movimentoUrso != null)
        {
            if (movimentoUrso.EstaNaAgua())
            {
                SetLayerUrsoNaAgua();
            }
            else
            {
                SetLayerUrsoNoGelo();
            }
        }
    }
}