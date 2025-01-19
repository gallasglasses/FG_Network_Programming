using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Destroyer : MonoBehaviour
{
    private void Awake()
    {
        if (NetworkManager.Singleton != null)
        {
            Destroy(NetworkManager.Singleton.gameObject);
        }

        if (MultiplayerManager.Instance != null)
        {
            Destroy(MultiplayerManager.Instance.gameObject);
        }

        if (GameLobby.Instance != null)
        {
            Destroy(GameLobby.Instance.gameObject);
        }

        //if (CharacterSelectReady.Instance != null)
        //{
        //    Destroy(CharacterSelectReady.Instance.gameObject);
        //}
    }

}