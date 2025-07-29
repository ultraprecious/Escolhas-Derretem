using UnityEngine;
using System.Collections; // Este using n�o � estritamente necess�rio para este c�digo, mas pode ser �til para outras funcionalidades
using System.Collections.Generic; // Este using n�o � estritamente necess�rio para este c�digo, mas pode ser �til para outras funcionalidades
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // M�todo para iniciar o jogo
    public void PlayGame()
    {
        Debug.Log("Iniciando jogo: CutsceneInicial"); // Adicionado para depura��o
        Time.timeScale = 1f; // Garante que o tempo do jogo est� normalizado (�til se estiver pausado)
        SceneManager.LoadScene("Jogo");
    }

    // M�todo para carregar a cena de Cr�ditos
    public void Cr�ditos()
    {
        Debug.Log("Carregando cena: Cr�ditos"); // Adicionado para depura��o
        SceneManager.LoadScene("Cr�ditos");
    }

    // M�todo para carregar a cena de Personagens
    public void Personagens()
    {
        Debug.Log("Carregando cena: Personagens"); // Adicionado para depura��o
        SceneManager.LoadScene("Personagens");
    }

    // NOVO M�TODO: Para sair da aplica��o
    public void SairAplicacao()
    {
        Debug.Log("Saindo da aplica��o...");

        // Verifica se a aplica��o est� rodando no editor do Unity
        // ou em um build final (Windows, Mac, Android, etc.)
#if UNITY_EDITOR
            // Se estiver no Editor, para a execu��o do jogo
            UnityEditor.EditorApplication.isPlaying = false;
#else
        // Se estiver em um build final, encerra a aplica��o
        Application.Quit();
#endif
    }
}