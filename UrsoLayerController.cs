using UnityEngine;
using System.Collections; // Necess�rio para coroutines, se for usar fading de layer

public class UrsoLayerController : MonoBehaviour
{
    [Header("Configura��es de Layers")]
    [Tooltip("O Sorting Layer para quando o urso estiver no gelo.")]
    [SerializeField] private string layerGeloSortingLayerName = "Personagem"; // Nome do seu Sorting Layer para personagens no gelo
    [Tooltip("A Ordem em Layer para quando o urso estiver no gelo (maior = mais acima).")]
    [SerializeField] private int layerGeloOrderInLayer = 0; // Ordem em Layer para o urso no gelo

    [Tooltip("O Sorting Layer para quando o urso estiver na �gua (para parecer submerso).")]
    [SerializeField] private string layerAguaSortingLayerName = "Agua"; // Nome do seu Sorting Layer para objetos submersos na �gua
    [Tooltip("A Ordem em Layer para quando o urso estiver na �gua (menor = mais abaixo).")]
    [SerializeField] private int layerAguaOrderInLayer = -1; // Ordem em Layer para o urso na �gua (abaixo do gelo/personagens)

    [Tooltip("O Layer de F�sica 2D para quando o urso estiver no gelo (para colis�es).")]
    [SerializeField] private int physicsLayerGelo = 8; // Ex: Layer "Urso"
    [Tooltip("O Layer de F�sica 2D para quando o urso estiver na �gua (para evitar colis�es com gelo se necess�rio).")]
    [SerializeField] private int physicsLayerAgua = 4; // Ex: Layer "UrsoAgua" ou "Default" se n�o houver colis�es

    [Header("Componentes Necess�rios")]
    [SerializeField] private SpriteRenderer ursoSpriteRenderer;
    [SerializeField] private GameObject ursoGameObject; // Refer�ncia ao GameObject do urso para alterar o layer de f�sica

    private MovimentoUrso movimentoUrso; // Para checar o estado de 'estaNaAgua'

    void Awake()
    {
        if (ursoSpriteRenderer == null)
        {
            ursoSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (ursoSpriteRenderer == null)
            {
                Debug.LogError("UrsoLayerController: SpriteRenderer do urso n�o atribu�do e n�o encontrado nos filhos.", this);
                enabled = false;
                return;
            }
        }

        if (ursoGameObject == null)
        {
            ursoGameObject = this.gameObject; // Assume que este script est� no GameObject raiz do urso
        }

        movimentoUrso = GetComponent<MovimentoUrso>();
        if (movimentoUrso == null)
        {
            Debug.LogError("UrsoLayerController: MovimentoUrso n�o encontrado no mesmo GameObject ou nos pais.", this);
            enabled = false;
            return;
        }

        // Garante que o urso comece na layer correta
        SetLayerUrsoNoGelo();
    }

    // Chamado para colocar o urso na layer de "gelo" (acima da �gua)
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
        Debug.Log("[UrsoLayerController] Urso est� na Layer de Gelo (visualmente e fisicamente).");
    }

    // Chamado para colocar o urso na layer de "�gua" (submerso)
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
        Debug.Log("[UrsoLayerController] Urso est� na Layer de �gua (visualmente e fisicamente).");
    }

    // M�todo para ser chamado externamente (QuizManager ou MovimentoUrso)
    // Este m�todo pode ser chamado quando o estado do urso muda.
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