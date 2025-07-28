using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class QuizManager : MonoBehaviour
{
    // Elementos da UI
    public Text questionText;
    public Button[] answerButtons;
    public Text feedbackText;
    public Text scoreText;
    public Text progressText;
    public Text environmentHealthText;
    public Text timeRemainingText;

    // Dados do Quiz
    public QuestionData[] allQuestions;

    // Integração com o Urso e Cenário
    public MovimentoUrso ursoMovimento;
    public ParallaxLayer[] parallaxLayers;

    // Configurações de Feedback e Cooldown
    public float feedbackErrorCooldown = 1.0f;
    public float nextQuestionArrivalCooldown = 1.0f;
    [SerializeField] private float recoverySuccessCooldown = 2.0f;
    // REMOVIDO: [SerializeField] private float iceMeltDuration = 1.5f; 

    // Variáveis de controle interno do quiz
    private List<QuestionData> selectedQuestions;
    private int currentQuestionIndex = 0;
    private int score = 0;
    private bool canAnswer = true;
    private bool quizActive = true;
    private bool esperandoResposta = false;
    private bool isRequestingNextQuestion = false;

    // Variáveis de control de jogo (saúde, tempo, penalidades)
    public int numberOfQuestionsPerQuiz = 15;
    public int initialEnvironmentHealth = 100;
    private int currentEnvironmentHealth;
    public int healthPenaltyPerWrongAnswer = 10;
    public float initialTimeInSeconds = 300f;
    private float currentTimeRemaining;
    private int consecutiveWrongAnswers = 0;
    public int maxProgressivePenaltyErrors = 5;


    void Start()
    {
        // Validações iniciais
        if (allQuestions == null || allQuestions.Length == 0) { Debug.LogError("Nenhuma pergunta atribuída."); enabled = false; return; }
        if (answerButtons == null || answerButtons.Length != 2) { Debug.LogError("Espera 2 botões de resposta."); enabled = false; return; }
        if (ursoMovimento == null) Debug.LogError("Campo 'Urso Movimento' não atribuído.");
        if (parallaxLayers == null || parallaxLayers.Length == 0) Debug.LogWarning("Nenhum ParallaxLayer atribuído.");
        if (feedbackText == null) Debug.LogWarning("FeedbackText não atribuído.");
        if (scoreText == null) Debug.LogWarning("ScoreText não atribuído.");
        if (progressText == null) Debug.LogWarning("ProgressText não atribuído.");
        if (environmentHealthText == null) Debug.LogWarning("EnvironmentHealthText não atribuído.");
        if (timeRemainingText == null) Debug.LogWarning("TimeRemainingText não atribuído.");

        currentEnvironmentHealth = initialEnvironmentHealth;
        currentTimeRemaining = initialTimeInSeconds;
        consecutiveWrongAnswers = 0;

        PrepareQuizQuestions();

        for (int i = 0; i < answerButtons.Length; i++)
        {
            int buttonIndex = i;
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(buttonIndex));
        }

        if (feedbackText != null) feedbackText.gameObject.SetActive(false);

        SetCenarioMovimento(false);
        LoadQuestion();
        UpdateGameUI();
    }

    void Update()
    {
        if (quizActive)
        {
            currentTimeRemaining -= Time.deltaTime;
            if (currentTimeRemaining <= 0)
            {
                currentTimeRemaining = 0;
                quizActive = false;
                EndGame(true);
            }
            UpdateGameUI();
        }
    }

    void PrepareQuizQuestions()
    {
        List<QuestionData> availableQuestions = new List<QuestionData>(allQuestions);
        for (int i = 0; i < availableQuestions.Count; i++)
        {
            QuestionData temp = availableQuestions[i];
            int randomIndex = Random.Range(i, availableQuestions.Count);
            availableQuestions[i] = availableQuestions[randomIndex];
            availableQuestions[randomIndex] = temp;
        }

        selectedQuestions = new List<QuestionData>();
        for (int i = 0; i < numberOfQuestionsPerQuiz && i < availableQuestions.Count; i++)
        {
            selectedQuestions.Add(availableQuestions[i]);
        }

        if (selectedQuestions.Count == 0)
        {
            Debug.LogError("Nenhuma pergunta foi selecionada.");
            enabled = false;
        }
    }

    void LoadQuestion()
    {
        if (currentQuestionIndex >= selectedQuestions.Count)
        {
            EndGame(false);
            return;
        }

        QuestionData currentQuestion = selectedQuestions[currentQuestionIndex];
        questionText.text = currentQuestion.questionText;

        if (feedbackText != null) feedbackText.gameObject.SetActive(false);

        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].gameObject.SetActive(true);
            answerButtons[i].GetComponentInChildren<Text>().text = currentQuestion.answers[i];
            answerButtons[i].interactable = true;
        }

        canAnswer = true;
        esperandoResposta = true;
        SetCenarioMovimento(false);
        if (ursoMovimento != null) ursoMovimento.SetEsperandoNovaBanquisa(false);
        UpdateGameUI();
    }

    public void OnAnswerSelected(int selectedAnswerIndex)
    {
        if (!canAnswer || !quizActive || !esperandoResposta) return;

        canAnswer = false;
        esperandoResposta = false;

        foreach (Button btn in answerButtons)
        {
            btn.interactable = false;
        }

        // SetCenarioMovimento(false); // MOVIDO para HandleIncorrectAnswerAndMelting() ou já tratado por LoadQuestion()

        QuestionData currentQuestion = selectedQuestions[currentQuestionIndex];

        if (selectedAnswerIndex == currentQuestion.correctAnswerIndex) // Resposta Correta
        {
            score++;
            consecutiveWrongAnswers = 0;

            if (feedbackText != null)
            {
                feedbackText.text = "Certo!";
                feedbackText.color = Color.green;
                feedbackText.gameObject.SetActive(true);
            }

            if (ursoMovimento != null)
            {
                Debug.Log($"[QuizManager.OnAnswerSelected - Acerto] - ESTADO DO URSO NO CHECK: ursoMovimento.EstaNaAgua() = {ursoMovimento.EstaNaAgua()}");

                if (ursoMovimento.EstaNaAgua())
                {
                    Debug.Log("[QuizManager.OnAnswerSelected] - Lógica: Acertou e estava na água. RESTAURANDO E TELEPORTANDO. CENÁRIO CONTINUA PARADO.");
                    BanquisaController banquisaParaRestaurar = ursoMovimento.GetUltimaBanquisaDeGeloAtiva();
                    if (banquisaParaRestaurar != null)
                    {
                        banquisaParaRestaurar.RestaurarBanquisa();
                        ursoMovimento.TeleportarUrsoParaBanquisa(banquisaParaRestaurar);
                        ursoMovimento.SetEsperandoNovaBanquisa(false);
                    }
                    else
                    {
                        Debug.LogWarning("Urso na água mas sem banquisa anterior para restaurar. Avançando pergunta sem movimento.");
                    }
                    StartCoroutine(AdvanceQuestionAfterRecoverySuccess());
                }
                else
                {
                    Debug.Log("[QuizManager.OnAnswerSelected] - Lógica: Acertou em terra firme. Urso/Cenário VÃO começar a se mover.");
                    SetCenarioMovimento(true);
                    ursoMovimento.SetEsperandoNovaBanquisa(true);
                }
            }
            else
            {
                Debug.LogWarning("MovimentoUrso não atribuído. Avançando pergunta sem mecânica do urso.");
                currentQuestionIndex++;
                LoadQuestion();
            }
        }
        else // Resposta Incorreta
        {
            StartCoroutine(HandleIncorrectAnswerAndMelting());
        }

        UpdateGameUI();
    }

    // Coroutine para lidar com a resposta incorreta, derretimento e queda do urso
    private IEnumerator HandleIncorrectAnswerAndMelting()
    {
        // Aplica penalidades imediatamente
        currentEnvironmentHealth -= healthPenaltyPerWrongAnswer;
        if (currentEnvironmentHealth < 0) currentEnvironmentHealth = 0;

        consecutiveWrongAnswers++;
        float penaltyFraction = (float)consecutiveWrongAnswers / maxProgressivePenaltyErrors;
        if (penaltyFraction > 1.0f) penaltyFraction = 1.0f;

        currentTimeRemaining -= currentTimeRemaining * penaltyFraction;
        if (currentTimeRemaining < 0) currentTimeRemaining = 0;

        // Exibe feedback "Errado!"
        if (feedbackText != null)
        {
            string penaltyText = $" (-{Mathf.RoundToInt(penaltyFraction * 100)}% do tempo)";
            feedbackText.text = "Errado!" + penaltyText;
            feedbackText.color = Color.red;
            feedbackText.gameObject.SetActive(true);
        }

        // Inicia o derretimento do gelo no BanquisaController.
        // O BanquisaController agora controla o timing da desativação do colisor.
        if (ursoMovimento != null)
        {
            Transform plataformaAtualTransform = ursoMovimento.GetCurrentGroundedPlatform();
            if (plataformaAtualTransform != null)
            {
                BanquisaController banquisaAtual = plataformaAtualTransform.GetComponent<BanquisaController>();
                if (banquisaAtual != null)
                {
                    banquisaAtual.DerreterBanquisa();
                    Debug.Log($"[QuizManager] Banquisa '{banquisaAtual.name}' começou a derreter. Urso ainda está em cima.");
                }
                else
                {
                    Debug.LogWarning($"A banquisa '{plataformaAtualTransform.name}' não possui um BanquisaController anexado.", plataformaAtualTransform.gameObject);
                }
            }
            else
            {
                Debug.LogWarning("O urso não está em nenhuma banquisa detectável para derreter.");
            }
            ursoMovimento.SetEsperandoNovaBanquisa(false);
        }

        // AQUI ESTÁ A MUDANÇA PRINCIPAL: Esperamos o TEMPO DE DERRETIMENTO DO GELO
        // Este tempo deve corresponder ao 'meltDuration' no BanquisaController.
        // Isso permite que a animação de derretimento aconteça ANTES da queda.
        if (ursoMovimento != null && ursoMovimento.GetCurrentGroundedPlatform() != null)
        {
            // Se o urso está em uma banquisa, usamos o meltDuration dela
            BanquisaController bc = ursoMovimento.GetCurrentGroundedPlatform().GetComponent<BanquisaController>();
            if (bc != null)
            {
                yield return new WaitForSeconds(bc.meltDuration); // Espera o tempo de derretimento da banquisa
            }
            else
            {
                // Fallback caso a banquisa não tenha BanquisaController (mas a mensagem de aviso já existiria)
                yield return new WaitForSeconds(1.5f); // Tempo padrão se não conseguir o BanquisaController
            }
        }
        else
        {
            // Se o urso não está em nenhuma banquisa (já caindo ou no ar), não há gelo para derreter, apenas espera um tempo padrão.
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("[QuizManager] Tempo de derretimento do gelo concluído. Parando cenário para o urso cair.");

        // Agora, o cenário para, permitindo que o urso caia se o colisor da banquisa foi desativado.
        SetCenarioMovimento(false);

        // Espera o cooldown de feedback de erro antes de avançar a pergunta
        yield return new WaitForSeconds(feedbackErrorCooldown);

        if (!quizActive) yield break;
        currentQuestionIndex++;
        LoadQuestion();

        // Verifica condições de fim de jogo após o processo
        if (currentEnvironmentHealth <= 0)
        {
            quizActive = false;
            EndGame(true);
            yield break;
        }
        if (currentTimeRemaining <= 0)
        {
            currentTimeRemaining = 0;
            quizActive = false;
            EndGame(true);
            yield break;
        }
    }

    // Esta coroutine não será mais chamada diretamente para erros,
    // pois HandleIncorrectAnswerAndMelting a substitui.
    private IEnumerator AdvanceQuestionAfterErrorFeedback() { yield break; } // Pode ser removida ou deixada assim.

    private IEnumerator AdvanceQuestionAfterRecoverySuccess()
    {
        isRequestingNextQuestion = true;
        Debug.Log("QuizManager: Acerto de recuperação. Cooldown de recuperação iniciado.");
        yield return new WaitForSeconds(recoverySuccessCooldown);
        isRequestingNextQuestion = false;

        if (!quizActive) yield break;

        Debug.Log("Cooldown de recuperação finalizado. Avançando para a próxima pergunta.");
        currentQuestionIndex++;
        LoadQuestion();
    }

    public void RequestNextQuestion()
    {
        if (!quizActive || isRequestingNextQuestion) return;

        if (ursoMovimento != null && ursoMovimento.EstaNaAgua())
        {
            Debug.LogWarning("RequestNextQuestion() chamado enquanto o urso está na água. Ignorando.");
            SetCenarioMovimento(false);
            return;
        }

        StartCoroutine(AdvanceQuestionAfterArrivalCooldown());
    }

    private IEnumerator AdvanceQuestionAfterArrivalCooldown()
    {
        isRequestingNextQuestion = true;
        Debug.Log("QuizManager recebeu pedido para avançar. Cooldown de chegada iniciado.");
        yield return new WaitForSeconds(nextQuestionArrivalCooldown);
        isRequestingNextQuestion = false;

        if (!quizActive) yield break;

        Debug.Log("Cooldown de chegada finalizado. Avançando para a próxima pergunta.");
        currentQuestionIndex++;
        LoadQuestion();
    }

    void UpdateGameUI()
    {
        if (scoreText != null) scoreText.text = "Pontuação: " + score;
        if (progressText != null) progressText.text = (currentQuestionIndex + 1) + "/" + selectedQuestions.Count;
        if (environmentHealthText != null) environmentHealthText.text = currentEnvironmentHealth + "%";

        if (timeRemainingText != null)
        {
            int minutes = Mathf.FloorToInt(currentTimeRemaining / 60);
            int seconds = Mathf.FloorToInt(currentTimeRemaining % 60);
            timeRemainingText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    void EndGame(bool gameOverByCondition)
    {
        quizActive = false;
        SetCenarioMovimento(false);

        if (questionText != null) questionText.gameObject.SetActive(false);
        foreach (Button btn in answerButtons)
        {
            btn.gameObject.SetActive(false);
        }
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);

        if (scoreText != null)
        {
            string finalMessage = "Jogo Concluído!";
            if (gameOverByCondition)
            {
                finalMessage = "Fim de Jogo!";
                if (currentEnvironmentHealth <= 0) finalMessage += "\nSaúde do Ambiente Esgotada!";
                if (currentTimeRemaining <= 0) finalMessage += "\nTempo Esgotado!";
            }
            scoreText.text = finalMessage + "\nSua Pontuação Final: " + score + "/" + selectedQuestions.Count;
            scoreText.gameObject.SetActive(true);
        }
        if (progressText != null) progressText.gameObject.SetActive(false);
        if (environmentHealthText != null) environmentHealthText.gameObject.SetActive(false);
        if (timeRemainingText != null) timeRemainingText.gameObject.SetActive(false);

        Debug.Log("Jogo de Perguntas e Respostas Concluído! Pontuação Final: " + score + "/" + selectedQuestions.Count);
    }

    private void SetCenarioMovimento(bool podeMover)
    {
        if (ursoMovimento != null)
        {
            ursoMovimento.SetPodeMoverCenario(podeMover);
        }

        if (parallaxLayers != null)
        {
            foreach (ParallaxLayer layer in parallaxLayers)
            {
                if (layer != null)
                {
                    layer.SetPodeMover(podeMover);
                }
            }
        }
    }
}