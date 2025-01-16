using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TestingNetcodeUI : MonoBehaviour
{
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button startServerButton;
    [SerializeField] private Button startClientButton;
    [SerializeField] private GameObject horizontalLayout;
    [SerializeField] private TextMeshProUGUI playersInGameText;

    private void Awake()
    {
        startHostButton.onClick.AddListener(() =>
        {
            Debug.Log("HOST");
            //NetworkManager.Singleton.StartHost();
            MultiplayerManager.Instance.StartHost();
            Hide();
        });

        startServerButton.onClick.AddListener(() =>
        {
            Debug.Log("SERVER");
            NetworkManager.Singleton.StartServer();
            Hide();
        });

        startClientButton.onClick.AddListener(() =>
        {
            Debug.Log("CLIENT");
            //NetworkManager.Singleton.StartClient();
            MultiplayerManager.Instance.StartClient();
            Hide();
        });
    }

    private void Update()
    {
        playersInGameText.text = $"Players in game: {MultiplayerManager.Instance.PlayersInGame}";
    }

    private void Hide()
    {
        horizontalLayout.SetActive(false);
    }
}
