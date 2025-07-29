using UnityEngine;

[CreateAssetMenu(fileName = "NewQuestion", menuName = "Quiz/Question Data")]
public class QuestionData : ScriptableObject
{
    [TextArea(3, 5)] // Permite que o texto da pergunta ocupe múltiplas linhas no Inspector.
    public string questionText;

    [Tooltip("As duas opções de resposta. O Elemento 0 é para o primeiro botão, e o Elemento 1 para o segundo.")]
    public string[] answers = new string[2]; // Array fixo para 2 respostas.

    [Tooltip("O índice da resposta correta: 0 para a primeira resposta, 1 para a segunda.")]
    [Range(0, 1)] // Garante que você só possa selecionar 0 ou 1 no Inspector.
    public int correctAnswerIndex; // O índice da resposta correta no array 'answers'.
}