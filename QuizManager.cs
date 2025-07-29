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

    // Integra��o com o Urso e Cen�rio
    public MovimentoUrso ursoMovimento;
    public ParallaxLayer[] parallaxLayers;

    // Configura��es de Feedback e Cooldown
    public float feedbackErrorCooldown = 1.0f;
    public float nextQuestionArrivalCooldown = 1.0f;
    [SerializeField] private float recoverySuccessCooldown = 2.0f;
    // REMOVIDO: [SerializeField] private float iceMeltDuration = 1.5f; 

    // Vari�veis de controle interno do quiz
    private List<QuestionData> selectedQuestions;
    private int currentQuestionIndex = 0;
    private int score = 0;
    private bool canAnswer = true;
    private bool quizActive = true;
    private bool esperandoResposta = false;
    private bool isRequestingNextQuestion = false;

    // Vari�veis de control de jogo (sa�de, tempo, penalidades)
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
        // Valida��es iniciais
        if (allQuestions == null || allQuestions.Length == 0) { Debug.LogError("Nenhuma pergunta atribu�da."); enabled = false; return; }
        if (answerButtons == null || answerButtons.Length != 2) { Debug.LogError("Espera 2 bot�es de resposta."); enabled = false; return; }
        if (ursoMovimento == null) Debug.LogError("Campo 'Urso Movimento' n�o atribu�do.");
        if (parallaxLayers == null || parallaxLayers.Length == 0) Debug.LogWarning("Nenhum ParallaxLayer atribu�do.");
        if (feedbackText == null) Debug.LogWarning("FeedbackText n�o atribu�do.");
        if (scoreText == null) Debug.LogWarning("ScoreText n�o atribu�do.");
        if (progressText == null) Debug.LogWarning("ProgressText n�o atribu�do.");
        if (environmentHealthText == null) Debug.LogWarning("EnvironmentHealthText n�o atribu�do.");
        if (timeRemainingText == null) Debug.LogWarning("TimeRemainingText n�o atribu�do.");

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

        // SetCenarioMovimento(false); // MOVIDO para HandleIncorrectAnswerAndMelting() ou j� tratado por LoadQuestion()

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
                    Debug.Log("[QuizManager.OnAnswerSelected] - L�gica: Acertou e estava na �gua. RESTAURANDO E TELEPORTANDO. CEN�RIO CONTINUA PARADO.");
                    BanquisaController banquisaParaRestaurar = ursoMovimento.GetUltimaBanquisaDeGeloAtiva();
                    if (banquisaParaRestaurar != null)
                    {
                        banquisaParaRestaurar.RestaurarBanquisa();
                        ursoMovimento.TeleportarUrsoParaBanquisa(banquisaParaRestaurar);
                        ursoMovimento.SetEsperandoNovaBanquisa(false);
                    }
                    else
                    {
                        Debug.LogWarning("Urso na �gua mas sem banquisa anterior para restaurar. Avan�ando pergunta sem movimento.");
                    }
                    StartCoroutine(AdvanceQuestionAfterRecoverySuccess());
                }
                else
                {
                    Debug.Log("[QuizManager.OnAnswerSelected] - L�gica: Acertou em terra firme. Urso/Cen�rio V�O come�ar a se mover.");
                    SetCenarioMovimento(true);
                    ursoMovimento.SetEsperandoNovaBanquisa(true);
                }
            }
            else
            {
                Debug.LogWarning("MovimentoUrso n�o atribu�do. Avan�ando pergunta sem mec�nica do urso.");
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
        // O BanquisaController agora controla o timing da desativa��o do colisor.
        if (ursoMovimento != null)
        {
            Transform plataformaAtualTransform = ursoMovimento.GetCurrentGroundedPlatform();
            if (plataformaAtualTransform != null)
            {
                BanquisaController banquisaAtual = plataformaAtualTransform.GetComponent<BanquisaController>();
                if (banquisaAtual != null)
                {
                    banquisaAtual.DerreterBanquisa();
                    Debug.Log($"[QuizManager] Banquisa '{banquisaAtual.name}' come�ou a derreter. Urso ainda est� em cima.");
                }
                else
                {
                    Debug.LogWarning($"A banquisa '{plataformaAtualTransform.name}' n�o possui um BanquisaController anexado.", plataformaAtualTransform.gameObject);
                }
            }
            else
            {
                Debug.LogWarning("O urso n�o est� em nenhuma banquisa detect�vel para derreter.");
            }
            ursoMovimento.SetEsperandoNovaBanquisa(false);
        }

        // AQUI EST� A MUDAN�A PRINCIPAL: Esperamos o TEMPO DE DERRETIMENTO DO GELO
        // Este tempo deve corresponder ao 'meltDuration' no BanquisaController.
        // Isso permite que a anima��o de derretimento aconte�a ANTES da queda.
        if (ursoMovimento != null && ursoMovimento.GetCurrentGroundedPlatform() != null)
        {
            // Se o urso est� em uma banquisa, usamos o meltDuration dela
            BanquisaController bc = ursoMovimento.GetCurrentGroundedPlatform().GetComponent<BanquisaController>();
            if (bc != null)
            {
                yield return new WaitForSeconds(bc.meltDuration); // Espera o tempo de derretimento da banquisa
            }
            else
            {
                // Fallback caso a banquisa n�o tenha BanquisaController (mas a mensagem de aviso j� existiria)
                yield return new WaitForSeconds(1.5f); // Tempo padr�o se n�o conseguir o BanquisaController
            }
        }
        else
        {
            // Se o urso n�o est� em nenhuma banquisa (j� caindo ou no ar), n�o h� gelo para derreter, apenas espera um tempo padr�o.
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("[QuizManager] Tempo de derretimento do gelo conclu�do. Parando cen�rio para o urso cair.");

        // Agora, o cen�rio para, permitindo que o urso caia se o colisor da banquisa foi desativado.
        SetCenarioMovimento(false);

        // Espera o cooldown de feedback de erro antes de avan�ar a pergunta
        yield return new WaitForSeconds(feedbackErrorCooldown);

        if (!quizActive) yield break;
        currentQuestionIndex++;
        LoadQuestion();

        // Verifica condi��es de fim de jogo ap�s o processo
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

    // Esta coroutine n�o ser� mais chamada diretamente para erros,
    // pois HandleIncorrectAnswerAndMelting a substitui.
    private IEnumerator AdvanceQuestionAfterErrorFeedback() { yield break; } // Pode ser removida ou deixada assim.

    private IEnumerator AdvanceQuestionAfterRecoverySuccess()
    {
        isRequestingNextQuestion = true;
        Debug.Log("QuizManager: Acerto de recupera��o. Cooldown de recupera��o iniciado.");
        yield return new WaitForSeconds(recoverySuccessCooldown);
        isRequestingNextQuestion = false;

        if (!quizActive) yield break;

        Debug.Log("Cooldown de recupera��o finalizado. Avan�ando para a pr�xima pergunta.");
        currentQuestionIndex++;
        LoadQuestion();
    }

    public void RequestNextQuestion()
    {
        if (!quizActive || isRequestingNextQuestion) return;

        if (ursoMovimento != null && ursoMovimento.EstaNaAgua())
        {
            Debug.LogWarning("RequestNextQuestion() chamado enquanto o urso est� na �gua. Ignorando.");
            SetCenarioMovimento(false);
            return;
        }

        StartCoroutine(AdvanceQuestionAfterArrivalCooldown());
    }

    private IEnumerator AdvanceQuestionAfterArrivalCooldown()
    {
        isRequestingNextQuestion = true;
        Debug.Log("QuizManager recebeu pedido para avan�ar. Cooldown de chegada iniciado.");
        yield return new WaitForSeconds(nextQuestionArrivalCooldown);
        isRequestingNextQuestion = false;

        if (!quizActive) yield break;

        Debug.Log("Cooldown de chegada finalizado. Avan�ando para a pr�xima pergunta.");
        currentQuestionIndex++;
        LoadQuestion();
    }

    void UpdateGameUI()
    {
        if (scoreText != null) scoreText.text = "Pontua��o: " + score;
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
            string finalMessage = "Jogo Conclu�do!";
            if (gameOverByCondition)
            {
                finalMessage = "Fim de Jogo!";
                if (currentEnvironmentHealth <= 0) finalMessage += "\nSa�de do Ambiente Esgotada!";
                if (currentTimeRemaining <= 0) finalMessage += "\nTempo Esgotado!";
            }
            scoreText.text = finalMessage + "\nSua Pontua��o Final: " + score + "/" + selectedQuestions.Count;
            scoreText.gameObject.SetActive(true);
        }
        if (progressText != null) progressText.gameObject.SetActive(false);
        if (environmentHealthText != null) environmentHealthText.gameObject.SetActive(false);
        if (timeRemainingText != null) timeRemainingText.gameObject.SetActive(false);

        Debug.Log("Jogo de Perguntas e Respostas Conclu�do! Pontua��o Final: " + score + "/" + selectedQuestions.Count);
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