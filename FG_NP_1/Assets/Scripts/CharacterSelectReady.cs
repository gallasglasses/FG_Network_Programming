using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;

public class CharacterSelectReady : NetworkBehaviour
{
    public static CharacterSelectReady Instance { get; private set; }

    public event EventHandler OnReadyChanged;

    private Dictionary<ulong, bool> playerReadyDictionary = new Dictionary<ulong, bool>();


    private void Awake()
    {
        if (Instance == null)
        {
            Debug.Log($"[Awake] Instance == null");
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.Log($"[Awake] Instance != null");
            Destroy(this.gameObject);
        }
    }

    public void SetPlayerReady()
    {
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        Debug.Log($"[SetPlayerReady] LocalClientId: {clientId}");
        SetPlayerReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        SetPlayerReadyClientRpc(clientId);
        Debug.Log($"[SetPlayerReadyServerRpc] Connected Clients: {string.Join(", ", NetworkManager.Singleton.ConnectedClientsIds)}");
        Debug.Log($"[SetPlayerReadyServerRpc] SenderClientId: {clientId}");


        if (!playerReadyDictionary.ContainsKey(clientId))
        {
            Debug.LogWarning($"[SetPlayerReadyServerRpc] Client {clientId} is not in playerReadyDictionary! Adding now.");
            playerReadyDictionary[clientId] = false;
        }
        playerReadyDictionary[clientId] = true;
        Debug.Log($"[SetPlayerReadyServerRpc] Client {clientId} marked as ready.");


        bool allClientsReady = true;
        foreach (ulong connectedClientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!playerReadyDictionary.ContainsKey(connectedClientId) || !playerReadyDictionary[connectedClientId])
            {
                // This player is NOT ready
                allClientsReady = false;
                break;
            }
        }

        if (allClientsReady)
        {
            GameLobby.Instance.DeleteLobby();
            Loader.LoadNetwork(Loader.Scene.GameScene);
        }
    }

    [ClientRpc]
    private void SetPlayerReadyClientRpc(ulong clientId)
    {
        if (!playerReadyDictionary.ContainsKey(clientId))
        {
            Debug.LogWarning($"[SetPlayerReadyClientRpc] Client {clientId} not found in dictionary on client. Adding now.");
            playerReadyDictionary[clientId] = false;
        }
        playerReadyDictionary[clientId] = true;
        Debug.Log($"[SetPlayerReadyClientRpc] Client {clientId} marked as ready on client.");

        OnReadyChanged?.Invoke(this, EventArgs.Empty);
    }


    public bool IsPlayerReady(ulong clientId)
    {
        if (!playerReadyDictionary.ContainsKey(clientId))
        {
            Debug.LogWarning($"[IsPlayerReady] Client {clientId} not found in playerReadyDictionary.");
            return false;
        }

        bool isReady = playerReadyDictionary[clientId];
        Debug.Log($"[IsPlayerReady] Client {clientId} ready status: {isReady}");
        return isReady;
    }

    //public override void OnDestroy()
    //{
    //    if (Instance == this)
    //    {
    //        Instance = null;
    //    }
    //}
}