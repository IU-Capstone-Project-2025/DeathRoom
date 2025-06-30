using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button singleplayerButton;
    [SerializeField] private Button multiplayerButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    private void Start()
    {
        singleplayerButton.onClick.AddListener(StartSingleplayer);
        multiplayerButton.onClick.AddListener(StartMultiplayer);
        settingsButton.onClick.AddListener(OpenSettings);
        exitButton.onClick.AddListener(ExitGame);

        if (singleplayerButton == null || multiplayerButton == null || 
            settingsButton == null || exitButton == null)
        {
            Debug.LogError("Кнопки не назначены.");
        }
    }

    private void StartSingleplayer()
    {
        SceneManager.LoadScene("PVP Arena 1");
    }

    private void StartMultiplayer()
    {
        //
    }

    private void OpenSettings()
    {
        // 
    }

    private void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}