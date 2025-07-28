using UnityEngine;
using System.Collections;

public class MovimentoUrso : MonoBehaviour
{
    [SerializeField] private float velocidadeCaminhadaDoMundo = 2f;
    [SerializeField] private float forcaPuloDaAgua = 10f;
    [SerializeField] private float distanciaChecagemChao = 0.1f;
    [SerializeField] private LayerMask camadaGelo;
    [SerializeField] private LayerMask camadaAgua;

    // Variáveis de debug para o Raycast horizontal/diagonal
    [SerializeField] private float distanciaChecagemProximaBanquisaDebug = 5f;
    [SerializeField] private float alturaOrigemRaycastProximaBanquisa = 0.5f;
    [SerializeField][Range(0f, 90f)] private float anguloRaycastProximaBanquisa = 30f;

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private float velocidadeHorizontalAtualParaMundo;
    private Transform currentGroundedPlatform = null;
    private bool podeMoverCenario = false;
    private bool estaNaAgua = false;

    private QuizManager quizManager;
    private bool esperandoNovaBanquisa = false;
    private BanquisaController ultimaBanquisaDeGeloAtiva = null;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        if (rb == null) Debug.LogError("Rigidbody2D is missing.");
        if (boxCollider == null) Debug.LogError("BoxCollider2D is missing.");
        velocidadeHorizontalAtualParaMundo = velocidadeCaminhadaDoMundo;
        quizManager = FindObjectOfType<QuizManager>();
        if (quizManager == null) Debug.LogError("QuizManager not found.");
    }

    void FixedUpdate()
    {
        Debug.Log($"[MovimentoUrso.FixedUpdate Start] - estaNaAgua: {estaNaAgua}, podeMoverCenario: {podeMoverCenario}, currentGroundedPlatform: {currentGroundedPlatform?.name ?? "None"}");

        Vector2 origemChecagem = new Vector2(boxCollider.bounds.center.x, boxCollider.bounds.min.y);
        RaycastHit2D hitChao = Physics2D.Raycast(origemChecagem, Vector2.down, distanciaChecagemChao, camadaGelo | camadaAgua);
        bool estaNoChao = (hitChao.collider != null);

        if (hitChao.collider != null)
        {
            Debug.Log($"[MovimentoUrso.Raycast] Atingiu: {hitChao.collider.gameObject.name} na Layer: {LayerMask.LayerToName(hitChao.collider.gameObject.layer)}");
        }
        else
        {
            Debug.Log("[MovimentoUrso.Raycast] Não atingiu nada abaixo.");
        }

        // Lógica para armazenar a última banquisa de gelo antes de cair na água
        // Isso deve ser feito ANTES que 'estaNaAgua' mude para true, ou seja, enquanto ele ainda está no gelo
        if (estaNoChao && !estaNaAgua)
        {
            // Verifica se a plataforma ATUAL é uma banquisa de gelo
            if (hitChao.collider.CompareTag("Gelo"))
            {
                BanquisaController banquisaAtual = hitChao.transform.GetComponent<BanquisaController>();
                if (banquisaAtual != null)
                {
                    ultimaBanquisaDeGeloAtiva = banquisaAtual; // Armazena a banquisa atual como a última ativa
                    // Debug.Log($"[MovimentoUrso] Última banquisa ativa atualizada para: {ultimaBanquisaDeGeloAtiva.name}");
                }
            }
        }

        if (estaNoChao)
        {
            bool estavaNaAguaAntes = estaNaAgua;
            estaNaAgua = (((1 << hitChao.collider.gameObject.layer) & camadaAgua) != 0);

            if (estaNaAgua && !estavaNaAguaAntes)
            {
                Debug.Log("[MovimentoUrso] Urso TRANSICIONOU PARA A ÁGUA. Parando cenário.");
                podeMoverCenario = false; // Garante que o cenário pare ao cair na água
            }
            else if (!estaNaAgua && estavaNaAguaAntes)
            {
                Debug.Log("[MovimentoUrso] Urso TRANSICIONOU PARA FORA DA ÁGUA (em contato com gelo).");
            }

            if (esperandoNovaBanquisa && currentGroundedPlatform != hitChao.transform && !estaNaAgua)
            {
                Debug.Log("Urso aterrisou em nova banquisa de gelo! Avançando pergunta...");
                esperandoNovaBanquisa = false;
                if (quizManager != null) quizManager.RequestNextQuestion();
            }
            currentGroundedPlatform = hitChao.transform;
        }
        else
        {
            currentGroundedPlatform = null;
        }

        if (!podeMoverCenario || estaNaAgua)
        {
            velocidadeHorizontalAtualParaMundo = 0f;
            if (estaNaAgua) rb.velocity = new Vector2(0, rb.velocity.y);
            Debug.Log($"[MovimentoUrso.FixedUpdate End] - Cenário PARADO OU Urso na Água. estaNaAgua: {estaNaAgua}, podeMoverCenario: {podeMoverCenario}");
            return;
        }
        velocidadeHorizontalAtualParaMundo = velocidadeCaminhadaDoMundo;
        Debug.Log($"[MovimentoUrso.FixedUpdate End] - Cenário em MOVIMENTO. estaNaAgua: {estaNaAgua}, podeMoverCenario: {podeMoverCenario}");

        DetectarBanquisaProxima();
    }

    private void DetectarBanquisaProxima()
    {
        Vector2 origemRaycast = new Vector2(boxCollider.bounds.max.x, boxCollider.bounds.min.y + alturaOrigemRaycastProximaBanquisa);
        Quaternion rotation = Quaternion.Euler(0, 0, -anguloRaycastProximaBanquisa);
        Vector2 direcaoRaycast = rotation * Vector2.right;
        RaycastHit2D hitDiagonal = Physics2D.Raycast(origemRaycast, direcaoRaycast, distanciaChecagemProximaBanquisaDebug, camadaGelo);
        Debug.DrawRay(origemRaycast, direcaoRaycast * distanciaChecagemProximaBanquisaDebug, hitDiagonal.collider != null ? Color.cyan : Color.red);
        // Os logs de debug específicos para a detecção horizontal foram removidos para reduzir a poluição do console,
        // mas o Debug.DrawRay permanece útil.
    }

    public void SetPodeMoverCenario(bool podeMover)
    {
        this.podeMoverCenario = podeMover;
        if (!podeMover)
        {
            velocidadeHorizontalAtualParaMundo = 0f;
            StopAllCoroutines();
        }
        else
        {
            // Se está na água e pode mover, significa que o QuizManager mandou sair da água
            if (estaNaAgua)
            {
                Debug.Log("[MovimentoUrso] SetPodeMoverCenario(true) - Urso estava na água, aplicando pulo para fora.");
                rb.velocity = new Vector2(rb.velocity.x, 0); // Zera velocidade vertical para pulo limpo
                rb.AddForce(Vector2.up * forcaPuloDaAgua, ForceMode2D.Impulse);
                estaNaAgua = false; // Não estará mais na água após o impulso
            }
            velocidadeHorizontalAtualParaMundo = velocidadeCaminhadaDoMundo;
            // 'esperandoNovaBanquisa' é controlada pelo QuizManager via SetEsperandoNovaBanquisa()
        }
    }

    // Método para teleportar o urso para uma banquisa específica (chamado pelo QuizManager)
    public void TeleportarUrsoParaBanquisa(BanquisaController banquisaAlvo)
    {
        if (banquisaAlvo == null)
        {
            Debug.LogWarning("Tentativa de teleportar urso para banquisa nula.");
            return;
        }

        Vector3 posicaoAlvo = banquisaAlvo.GetPosicaoTeleporteUrso();
        transform.position = posicaoAlvo;
        rb.velocity = Vector2.zero; // Zera a velocidade para evitar movimentos indesejados
        estaNaAgua = false; // Urso não está mais na água
        Debug.Log($"[MovimentoUrso] Urso TELEPORTADO para {banquisaAlvo.name}. estaNaAgua setado para FALSE.");
        // O movimento do cenário (podeMoverCenario) é controlado pelo QuizManager.
        // A flag esperandoNovaBanquisa é controlada pelo QuizManager.
    }

    // Método para o QuizManager controlar a flag 'esperandoNovaBanquisa'
    public void SetEsperandoNovaBanquisa(bool esperando)
    {
        esperandoNovaBanquisa = esperando;
        Debug.Log($"[MovimentoUrso] esperandoNovaBanquisa setado para: {esperando}");
    }

    public BanquisaController GetUltimaBanquisaDeGeloAtiva()
    {
        return ultimaBanquisaDeGeloAtiva;
    }

    public float GetVelocidadeHorizontalParaCenario()
    {
        return velocidadeHorizontalAtualParaMundo;
    }

    public bool EstaNaAgua()
    {
        return estaNaAgua;
    }

    // Este método é usado pelo QuizManager para saber qual banquisa derreter.
    // Garante que só retorne uma banquisa de gelo E se não estiver na água.
    public Transform GetCurrentGroundedPlatform()
    {
        if (currentGroundedPlatform != null && currentGroundedPlatform.CompareTag("Gelo") && !estaNaAgua)
        {
            return currentGroundedPlatform;
        }
        return null;
    }
}