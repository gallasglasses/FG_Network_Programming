using UnityEngine;
using Unity.Netcode;

public class Tile : NetworkBehaviour
{
    private int tileId;
    private Renderer tileRenderer;
    private ulong ownerClientId = ulong.MaxValue;

    public void Initialize(int id)
    {
        tileId = id;
        tileRenderer = GetComponent<Renderer>();

        if (tileRenderer == null)
        {
            Debug.LogError($"tileRenderer is null on Tile {tileId}");
            return;
        }
        //tileRenderer.material.color = Color.white;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (tileRenderer == null)
        {
            tileRenderer = GetComponent<Renderer>();
            if (tileRenderer == null)
            {
                Debug.LogError($"tileRenderer not found on Tile {tileId}");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        GamePlayer player = other.GetComponent<GamePlayer>();
        if (player != null)
        {
            NetworkObject playerNetworkObject = other.GetComponent<NetworkObject>();
            if (playerNetworkObject != null && IsServer)
            {
                ulong playerId = playerNetworkObject.OwnerClientId;
                Color playerColor = player.GetPlayerColor();
                CaptureTile(playerId, playerColor);
            }
        }
    }

    private void CaptureTile(ulong capturingPlayerId, Color playerColor)
    {
        if (!IsServer)
            return;

        //if (ownerClientId == capturingPlayerId)
        //    return;

        //ownerClientId = capturingPlayerId;
        //tileRenderer.material.color = playerColor;
        //Debug.Log($"Tile {tileId} captured by player {ownerClientId}");
        UpdateTileColorServerRpc(capturingPlayerId, playerColor);
    }

    [Rpc(SendTo.Server)]
    private void UpdateTileColorServerRpc(ulong capturingPlayerId, Color playerColor)
    {
        if (ownerClientId == capturingPlayerId)
            return;

        ownerClientId = capturingPlayerId;
        tileRenderer.material.color = playerColor;
        Debug.Log($"Tile {tileId} captured by player {ownerClientId}");

        tileRenderer.material.color = playerColor;

        UpdateTileColorClientRpc(playerColor);
    }


    [ClientRpc]
    private void UpdateTileColorClientRpc(Color newColor)
    {
        if (tileRenderer == null)
        {
            Debug.LogError($"tileRenderer is null on Tile {tileId}");
            return;
        }
        tileRenderer.material.color = newColor;
    }
}
