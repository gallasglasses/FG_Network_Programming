using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private int gridSize = 8;
    [SerializeField] private float spacing = 1.5f;

    private Dictionary<int, Tile> tiles = new Dictionary<int, Tile>();

    private static GridManager _instance;
    public static GridManager Instance { get { return _instance; } }
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
        //GenerateGrid();
    }

    public void GenerateGrid()
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Not host, skipping grid generation.");
            return;
        }

        Debug.Log("Starting grid generation.");
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                int nextTileId = x * gridSize + z;
                Vector3 position = new Vector3();
                position.x = (x * spacing) + spacing / 2.0f;
                position.y = -spacing / 2.0f;
                position.z = (z * spacing) + spacing / 2.0f;

                GameObject tileObject = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                Debug.Log($"Created tile at {position}");
                Tile tile = tileObject.GetComponent<Tile>();
                if (tile == null)
                {
                    Debug.LogError("Tile component is missing on tilePrefab.");
                    continue;
                }
                tile.Initialize(nextTileId);

                NetworkObject networkObject = tileObject.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    Debug.LogError("NetworkObject component is missing on tilePrefab.");
                    continue;
                }
                networkObject.Spawn();
                Debug.Log($"Spawned tile with ID: {nextTileId}");

                tiles.Add(nextTileId, tile);
                nextTileId++;
            }
        }
        Debug.Log("Grid generation complete.");
    }

    public Tile GetTileById(int id)
    {
        return tiles.ContainsKey(id) ? tiles[id] : null;
    }
}
