using Unity.VisualScripting;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayersManager : NetworkBehaviour
{
    private static PlayersManager _instance;
    public static PlayersManager Instance { get { return _instance; } }

    private NetworkVariable<int> _playersInGame = new NetworkVariable<int>();
    public int PlayersInGame {  get { return _playersInGame.Value; } }

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

    void Update()
    {
        
    }
}
