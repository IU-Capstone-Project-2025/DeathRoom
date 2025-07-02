using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject player;

    private MonoBehaviour playerMovement;
    private bool isPaused = false;

    private void Start()
    {
        pauseMenuUI.SetActive(false);
        playerMovement = player.GetComponent<PlayerMovement>();

        resumeButton.onClick.AddListener(Resume);
        quitButton.onClick.AddListener(QuitGame);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) 
                Resume();
            else 
                Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        pauseMenuUI.SetActive(true);

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        isPaused = false;
        pauseMenuUI.SetActive(false);

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void QuitGame()
    {
        Debug.Log("Выход из игры");
        SceneManager.LoadScene("Menu");
    }
}
