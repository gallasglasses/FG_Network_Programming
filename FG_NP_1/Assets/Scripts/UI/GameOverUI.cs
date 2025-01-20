using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Unity.Netcode;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private Transform playersList;
    [SerializeField] private GameObject playerTemplate;
    [SerializeField] private Button mainMenuButton;

    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() => {
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenuScene);
        });
    }

    void Start()
    {
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;

        Hide();
    }
    private void GameManager_OnStateChanged(object sender, System.EventArgs e)
    {
        if (GameManager.Instance.IsGameState(GameState.GameOver))
        {
            ShowResults();
            Show();
        }
        else
        {
            Hide();
        }
    }

    public void ShowResults()
    {
        Dictionary<ulong, int> results = new Dictionary<ulong, int>();
        foreach ( var data in GameManager.Instance.GetPlayerResultsList())
        {
            results.Add(data.clientId, data.capturedTiles);
        }
        //Dictionary<ulong, int> results = GameManager.Instance.GetAllCapturedTileCounts();
        var sortedResults = results.OrderByDescending(result => result.Value).ToList();

        foreach (Transform child in playersList)
        {
            Destroy(child.gameObject);
        }

        foreach (var result in sortedResults)
        {
            GameObject row = Instantiate(playerTemplate, playersList);
            row.gameObject.SetActive(true);

            PlayerResultUI resultUI = row.GetComponent<PlayerResultUI>();
            if (resultUI != null)
            {
                string name = MultiplayerManager.Instance.GetPlayerDataFromClientId(result.Key).playerName.ToString();
                resultUI.SetText(name, result.Value.ToString());
                Debug.Log($"Player: {name} | Result: {result.Value.ToString()}");
            }
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
