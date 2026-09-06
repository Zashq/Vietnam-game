using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene")]
    public string gameSceneName = "GameScene";  // állítsd a te játékscened nevére

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;

    void Start()
    {
        // biztos ami biztos: főmenü látszik, a többi off
        ShowMain();
        Time.timeScale = 1f; // ha netán paused state-ből jönnél vissza
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // --- BUTTON HOOKS ---

    public void StartGame()
    {
        // játék scene betöltése
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("QuitGame pressed");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void OpenOptions()
    {
        mainPanel.SetActive(false);
        optionsPanel.SetActive(true);
        creditsPanel.SetActive(false);
    }

    public void OpenCredits()
    {
        mainPanel.SetActive(false);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    public void ShowMain()
    {
        mainPanel.SetActive(true);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }
}
