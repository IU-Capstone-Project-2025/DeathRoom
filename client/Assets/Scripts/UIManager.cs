using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public Button connectButton;
    public Camera interCum;
    public Client client;

    void Start()
    {
        usernameInput.gameObject.SetActive(true);
        connectButton.gameObject.SetActive(true);
        interCum.gameObject.SetActive(true);
        connectButton.onClick.AddListener(OnConnectClicked);
    }

    void OnConnectClicked()
    {
        string inputName = usernameInput.text.Trim();
        if (!string.IsNullOrEmpty(inputName))
        {
            client.playerName = inputName;
            usernameInput.gameObject.SetActive(false);
            connectButton.gameObject.SetActive(false);
            interCum.gameObject.SetActive(false);
            client.ConnectToServer();
        }
    }
}
