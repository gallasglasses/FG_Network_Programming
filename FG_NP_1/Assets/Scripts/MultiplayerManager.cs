using Unity.VisualScripting;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using Unity.Services.Authentication;

public class MultiplayerManager : NetworkBehaviour
{
    public const int MAX_PLAYER_AMOUNT = 4;
    private const string PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER = "PlayerNameMultiplayer";

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector3 spawnPosition;
    [SerializeField] private List<Color> playerColorList;

    private NetworkList<PlayerData> playerDataNetworkList;
    private string playerName;

    private static MultiplayerManager _instance;
    public static MultiplayerManager Instance { get { return _instance; } }

    private NetworkVariable<int> _playersInGame = new NetworkVariable<int>();
    public int PlayersInGame {  get { return _playersInGame.Value; } }

    public event EventHandler OnTryingToJoinGame;
    public event EventHandler OnFailedToJoinGame;
    public event EventHandler OnPlayerDataNetworkListChanged;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }

        playerName = PlayerPrefs.GetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, "GamePlayer" + OwnerClientId);
        playerDataNetworkList = new NetworkList<PlayerData>();

        playerDataNetworkList.OnListChanged += PlayerDataNetworkList_OnListChanged;
    }

    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            if(IsServer)
            {
                Debug.Log($"{id} just connected...");
                _playersInGame.Value++;
            }
        };

        NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
        {
            if(IsServer)
            {
                Debug.Log($"{id} just disconnected...");
                _playersInGame.Value--;
            }
        };


    }
    public string GetPlayerName()
    {
        return playerName;
    }

    public void SetPlayerName(string playerName)
    {
        this.playerName = playerName;

        PlayerPrefs.SetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, playerName);
        //
        SetPlayerNameServerRpc(playerName);
    }

    private void PlayerDataNetworkList_OnListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
        //switch (changeEvent.Type)
        //{
        //    case NetworkListEvent<PlayerData>.EventType.Add:
        //        Debug.Log($"Player added: {changeEvent.Value}");
        //        // 
        //        break;

        //    case NetworkListEvent<PlayerData>.EventType.Remove:
        //        Debug.Log($"Player removed: {changeEvent.Value}");
        //        // 
        //        break;

        //    case NetworkListEvent<PlayerData>.EventType.Value:
        //        Debug.Log($"Player data updated: {changeEvent.Value}");
        //        //
        //        break;
        //}
    }

    public void StartHost()
    {
        //NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartHost();
        //GridManager.Instance.GenerateGrid();
    }

    private void NetworkManager_OnClientConnectedCallback(ulong clientId)
    {
        playerDataNetworkList.Add(new PlayerData
        {
            clientId = clientId,
            colorId = GetFirstUnusedColorId()
        });
        SetPlayerNameServerRpc(GetPlayerName());
        SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
    }

    private void NetworkManager_Server_OnClientDisconnectCallback(ulong clientId)
    {
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            PlayerData playerData = playerDataNetworkList[i];
            if (playerData.clientId == clientId)
            {
                playerDataNetworkList.RemoveAt(i);
            }
        }
    }
    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse)
    {
        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= MAX_PLAYER_AMOUNT)
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game is full";
            return;
        }

        connectionApprovalResponse.Approved = true;
    }

    public void StartClient()
    {
        OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);

        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Client_OnClientConnectedCallback;
        NetworkManager.Singleton.StartClient();
    }

    private void NetworkManager_Client_OnClientConnectedCallback(ulong clientId)
    {
        SetPlayerNameServerRpc(GetPlayerName());
        SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
    }
    private void NetworkManager_Client_OnClientDisconnectCallback(ulong clientId)
    {
        OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerNameServerRpc(string playerName, ServerRpcParams serverRpcParams = default)
    {
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.playerName = playerName;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerIdServerRpc(string playerId, ServerRpcParams serverRpcParams = default)
    {
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.playerId = playerId;

        playerDataNetworkList[playerDataIndex] = playerData;
    }
    public bool IsPlayerIndexConnected(int playerIndex)
    {
        return playerIndex < playerDataNetworkList.Count;
    }
    public PlayerData GetPlayerData()
    {
        return GetPlayerDataFromClientId(NetworkManager.Singleton.LocalClientId);
    }

    public int GetPlayerDataIndexFromClientId(ulong clientId)
    {
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            if (playerDataNetworkList[i].clientId == clientId)
            {
                return i;
            }
        }
        return -1;
    }

    public PlayerData GetPlayerDataFromClientId(ulong clientId)
    {
        foreach (PlayerData playerData in playerDataNetworkList)
        {
            if (playerData.clientId == clientId)
            {
                return playerData;
            }
        }
        return default;
    }
    public PlayerData GetPlayerDataFromPlayerIndex(int playerIndex)
    {
        return playerDataNetworkList[playerIndex];
    }

    public Color GetPlayerColor(int colorId)
    {
        if (colorId >= 0 && colorId < playerColorList.Count)
            return playerColorList[colorId];
        return Color.white;
    }

    public void ChangePlayerColor(int colorId)
    {
        ChangePlayerColorServerRpc(colorId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangePlayerColorServerRpc(int colorId, ServerRpcParams serverRpcParams = default)
    {
        if (!IsColorAvailable(colorId))
        {
            // Color not available
            return;
        }

        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.colorId = colorId;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    private bool IsColorAvailable(int colorId)
    {
        foreach (PlayerData playerData in playerDataNetworkList)
        {
            if (playerData.colorId == colorId)
            {
                return false;
            }
        }
        return true;
    }

    private int GetFirstUnusedColorId()
    {
        for (int i = 0; i < playerColorList.Count; i++)
        {
            if (IsColorAvailable(i))
            {
                Debug.Log("Color index : " + i);
                return i;
            }
        }
        return -1;
    }
    public void KickPlayer(ulong clientId)
    {
        NetworkManager.Singleton.DisconnectClient(clientId);
        NetworkManager_Server_OnClientDisconnectCallback(clientId);
    }
}
