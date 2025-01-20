using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System;
using Unity.Netcode;
using Unity.VisualScripting;

public class GameLobby : MonoBehaviour
{
    private const float MAX_HEARTBEAT_TIMER = 15f;
    private const float MAX_LIST_LOBBIES_UPDATE_TIMER = 1.1f;

    private static GameLobby _instance;
    public static GameLobby Instance { get { return _instance; } }

    public event EventHandler OnCreateLobbyStarted;
    public event EventHandler OnCreateLobbyFailed;
    public event EventHandler OnJoinStarted;
    public event EventHandler OnQuickJoinFailed;
    public event EventHandler OnJoinFailed;

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }

    private Lobby joinedLobby;
    private float heartbeatTimer;
    private float listLobbiesTimer;
    private string playerName;

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

        InitializeUnityAuthentication();
    }

    public async void Authentication(string playerName)
    {
        this.playerName = playerName;
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(playerName);
        // running multiple Builds on the same PC Unity services get different player IDs

        await UnityServices.InitializeAsync(initializationOptions);

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in. " + AuthenticationService.Instance.PlayerId);

            ListLobbies();
            UpdateLobbyPlayers();
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private async void InitializeUnityAuthentication()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions initializationOptions = new InitializationOptions();
            initializationOptions.SetProfile(UnityEngine.Random.Range(0, 10000).ToString());
            // running multiple Builds on the same PC Unity services get different player IDs

            await UnityServices.InitializeAsync(initializationOptions);

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log("Signed in. " + AuthenticationService.Instance.PlayerId);

                ListLobbies();
                UpdateLobbyPlayers();
            };
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    //private async void Start()
    //{
    //    await UnityServices.InitializeAsync();

    //    AuthenticationService.Instance.SignedIn += () =>
    //    {
    //        Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
    //    };
    //    await AuthenticationService.Instance.SignInAnonymouslyAsync();
    //}

    void Update()
    {
        HandleLobbyHeartbeat();
        HandleUpdateListLobbies();
    }

    private async void HandleLobbyHeartbeat()
    {
        if (IsLobbyHost())
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                heartbeatTimer = MAX_HEARTBEAT_TIMER;
                try
                {
                    await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
                }
                catch (LobbyServiceException e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }

    private void HandleUpdateListLobbies()
    {
        if (IsJoinedLobby())
        {

            listLobbiesTimer -= Time.deltaTime;
            if (listLobbiesTimer <= 0f)
            {
                listLobbiesTimer = MAX_LIST_LOBBIES_UPDATE_TIMER;
                ListLobbies();
                UpdateLobbyPlayers();
            }
        }
    }
    public async void UpdateLobbyPlayers()
    {
        if (joinedLobby != null)
        {
            try
            {
                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogException(e);
            }
        }
    }

    [ContextMenu("Create Lobby")]
    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            CreateLobbyOptions newLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MultiplayerManager.MAX_PLAYER_AMOUNT, newLobbyOptions);
            joinedLobby = lobby;

            Debug.Log("Created lobby " + lobby.Name + " for " + lobby.MaxPlayers + " LobbyID " + lobby.Id + " Code: " + lobby.LobbyCode);
            PrintPlayers(joinedLobby);

            MultiplayerManager.Instance.StartHost();
            Loader.LoadNetwork(Loader.Scene.CharacterSelectScene);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
            OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    [ContextMenu("List Lobbies")]
    private async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions()
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs
            {
                lobbyList = queryResponse.Results
            });

            //test
            //Debug.Log("Lobbies found " + queryResponse.Results.Count);
            //foreach (Lobby lobby in queryResponse.Results)
            //{
            //    Debug.Log(lobby.Name + " " + lobby.MaxPlayers);
            //}
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }

    }

    [ContextMenu("Join Lobby By Code")]
    public async void JoinLobbyByCode(string lobbyCode)
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            if (string.IsNullOrWhiteSpace(lobbyCode))
            {
                Debug.LogWarning("Lobby code is not set or empty!");
                return;
            }

            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            
            MultiplayerManager.Instance.StartClient();
            Debug.Log("Joined Lobby By Code " + lobbyCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    [ContextMenu("Join Lobby By Id")]
    public async void JoinLobbyById(string lobbyId)
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(queryResponse.Results[0].Id);
            
            MultiplayerManager.Instance.StartClient();
            Debug.Log("Joined Lobby By Id ");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    [ContextMenu("Quick Join Lobby")]
    public async void QuickJoinLobby()
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            
            MultiplayerManager.Instance.StartClient();
            Debug.Log("Quick Joined Lobby");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
            OnQuickJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    [ContextMenu("Leave Lobby")]
    public async void LeaveLobby()
    {
        if (IsJoinedLobby())
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joinedLobby = null;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogException(e);
            }
        }
    }

    public async void KickPlayer(string playerId)
    {
        if (IsLobbyHost())
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
                Debug.Log(playerId + "was kicked from Lobby");
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    [ContextMenu("Delete Lobby")]
    public async void DeleteLobby()
    {
        if (IsLobbyHost())
        {
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
                Debug.Log("Lobby " + joinedLobby.Name + "was deleted");
                joinedLobby = null;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public async void MigrateLobbyHost(string newHostId)
    {
        if (joinedLobby == null || string.IsNullOrWhiteSpace(newHostId))
        {
            Debug.LogWarning("Cannot migrate host. Lobby is null or newHostId is invalid.");
            return;
        }
        try
        {
            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                HostId = newHostId
            });
            Debug.Log($"Host migrated to player with ID: {newHostId}");
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private void PrintPlayers(Lobby lobby)
    {
        Debug.Log("Players in Lobby " + lobby.Name);
        foreach (var player in lobby.Players)
        {
            Debug.Log(player.Id);
        }
    }

    private bool IsLobbyHost()
    {
        return IsJoinedLobby() && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private bool IsJoinedLobby()
    {
        return joinedLobby != null;
    }
    
    public bool IsHostPlayer(string id)
    {
        return IsJoinedLobby() && joinedLobby.HostId == id; // AuthenticationService.Instance.PlayerId - owner
    }

    public Lobby GetLobby()
    {
        return joinedLobby;
    }

    public List<Player> GetLobbyPlayers()
    {
        return IsJoinedLobby() && joinedLobby.Players != null
        ? joinedLobby.Players
        : new List<Player>();
    }
}
