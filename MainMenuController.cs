using UnityEngine;
using System.Collections; // Este using não é estritamente necessário para este código, mas pode ser útil para outras funcionalidades
using System.Collections.Generic; // Este using não é estritamente necessário para este código, mas pode ser útil para outras funcionalidades
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // Método para iniciar o jogo
    public void PlayGame()
    {
        Debug.Log("Iniciando jogo: CutsceneInicial"); // Adicionado para depuração
        Time.timeScale = 1f; // Garante que o tempo do jogo está normalizado (útil se estiver pausado)
        SceneManager.LoadScene("Jogo");
    }

    // Método para carregar a cena de Créditos
    public void Créditos()
    {
        Debug.Log("Carregando cena: Créditos"); // Adicionado para depuração
        SceneManager.LoadScene("Créditos");
    }

    // Método para carregar a cena de Personagens
    public void Personagens()
    {
        Debug.Log("Carregando cena: Personagens"); // Adicionado para depuração
        SceneManager.LoadScene("Personagens");
    }

    // NOVO MÉTODO: Para sair da aplicação
    public void SairAplicacao()
    {
        Debug.Log("Saindo da aplicação...");

        // Verifica se a aplicação está rodando no editor do Unity
        // ou em um build final (Windows, Mac, Android, etc.)
#if UNITY_EDITOR
            // Se estiver no Editor, para a execução do jogo
            UnityEditor.EditorApplication.isPlaying = false;
#else
        // Se estiver em um build final, encerra a aplicação
        Application.Quit();
#endif
    }
}