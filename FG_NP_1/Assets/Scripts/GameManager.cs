using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public event EventHandler OnStateChanged;
    public event EventHandler OnLocalPlayerReadyChanged;


    [SerializeField] private List<Vector3> playerSpawnPointList;
    [SerializeField] private GameObject playerPrefab;

    private NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.WaitingToStart);
    private NetworkVariable<float> countdownToStartTimer = new NetworkVariable<float>(3f);
    private NetworkVariable<float> gamePlayingTimer = new NetworkVariable<float>(0f);

    private float gameOverTimer = 10f;
    private bool isLocalPlayerReady;

    private Dictionary<ulong, bool> playerReadyDictionary = new Dictionary<ulong, bool>();
    private Dictionary<ulong, int> playerTileCounts = new Dictionary<ulong, int>();
    private NetworkList<PlayerResultData> playerResultsList;

    private void Awake()
    {
        Instance = this;

        playerResultsList = new NetworkList<PlayerResultData>();
    }

    void Start()
    {
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }
    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if (gameState.Value == GameState.WaitingToStart)
        {
            isLocalPlayerReady = true;
            OnLocalPlayerReadyChanged?.Invoke(this, EventArgs.Empty);

            SetPlayerReadyServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = true;

        bool allClientsReady = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId])
            {
                allClientsReady = false;
                break;
            }
        }

        if (allClientsReady)
        {
            gameState.Value = GameState.CountdownToStart;
        }
    }

    public override void OnNetworkSpawn()
    {
        gameState.OnValueChanged += State_OnValueChanged;

        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
            GridManager.Instance.GenerateGrid();
            playerSpawnPointList.Add(GridManager.Instance.GetTileById(0).GetComponent<Transform>().position);
            playerSpawnPointList.Add(GridManager.Instance.GetTileById(7).GetComponent<Transform>().position);
            playerSpawnPointList.Add(GridManager.Instance.GetTileById(56).GetComponent<Transform>().position);
            playerSpawnPointList.Add(GridManager.Instance.GetTileById(63).GetComponent<Transform>().position);
            for (int i = 0; i < playerSpawnPointList.Count; i++)
            {
                Vector3 spawnPoint = playerSpawnPointList[i];
                spawnPoint.y = 0f;
                playerSpawnPointList[i] = spawnPoint;
            }
        }
    }

    private void SceneManager_OnLoadEventCompleted(string scene, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        int count = 0;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            GameObject player = Instantiate(playerPrefab, playerSpawnPointList[count], Quaternion.identity);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
            //AddCapturedTile(clientId);
            playerResultsList.Add(new PlayerResultData
            {
                clientId = clientId,
                capturedTiles = 0
            });
            count++;
        }
    }
    private void State_OnValueChanged(GameState previousValue, GameState newValue)
    {
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    void Update()
    {
        if (!IsServer)
        {
            return;
        }

        switch (gameState.Value)
        {
            case GameState.WaitingToStart:
                break;
            case GameState.CountdownToStart:
                countdownToStartTimer.Value -= Time.deltaTime;
                if (countdownToStartTimer.Value < 0f)
                {
                    gameState.Value = GameState.GamePlaying;
                    gamePlayingTimer.Value = gameOverTimer;
                }
                break;
            case GameState.GamePlaying:
                gamePlayingTimer.Value -= Time.deltaTime;
                if (gamePlayingTimer.Value < 0f)
                {
                    gameState.Value = GameState.GameOver;
                }
                break;
            case GameState.GameOver:
                break;
        }
    }

    public bool IsGameState(GameState state)
    {
        bool isGameState = false;
        switch (state)
        {
            case GameState.WaitingToStart:
                isGameState = gameState.Value == GameState.WaitingToStart;
                break;
            case GameState.CountdownToStart:
                isGameState = gameState.Value == GameState.CountdownToStart;
                break;
            case GameState.GamePlaying:
                isGameState = gameState.Value == GameState.GamePlaying;
                break;
            case GameState.GameOver:
                isGameState = gameState.Value == GameState.GameOver;
                break;
        }
        return isGameState;
    }

    public bool IsLocalPlayerReady()
    {
        return isLocalPlayerReady;
    }

    public float GetGamePlayingTimePersent()
    {
        return 1 - (gamePlayingTimer.Value / gameOverTimer);
    }

    public float GetCountdownToStartTimer()
    {
        return countdownToStartTimer.Value;
    }

    public void AddCapturedTile(ulong playerId)
    {
        //if (!playerTileCounts.ContainsKey(playerId))
        //{
        //    playerTileCounts[playerId] = 0;
        //}

        //playerTileCounts[playerId]++;
        AddCapturedTileServerRpc(playerId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddCapturedTileServerRpc(ulong playerId)
    {
        int playerDataIndex = GetPlayerDataIndexFromClientId(playerId);
        PlayerResultData playerData = playerResultsList[playerDataIndex];
        playerData.capturedTiles += 1;
        playerResultsList[playerDataIndex] = playerData;
    }

    public int GetPlayerDataIndexFromClientId(ulong clientId)
    {
        for (int i = 0; i < playerResultsList.Count; i++)
        {
            if (playerResultsList[i].clientId == clientId)
            {
                return i;
            }
        }
        return -1;
    }

    //public int GetCapturedTileCount(ulong playerId)
    //{
    //    return playerTileCounts.ContainsKey(playerId) ? playerTileCounts[playerId] : 0;
    //}

    //public Dictionary<ulong, int> GetAllCapturedTileCounts()
    //{
    //    return new Dictionary<ulong, int>(playerTileCounts);
    //}

    public NetworkList<PlayerResultData> GetPlayerResultsList()
    {
        return playerResultsList;
    }
}

public enum GameState
{
    WaitingToStart,
    CountdownToStart,
    GamePlaying,
    GameOver,
}