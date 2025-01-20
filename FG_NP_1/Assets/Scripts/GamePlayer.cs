using System;
using System.Collections;
//using System.Drawing;
using Unity.Entities.UniversalDelegates;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class GamePlayer : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float raycastDistance = 10f;

    private PlayerVisual playerVisual;
    private WeaponComponent weapon;
    private CapsuleCollider capsuleCollider;
    private NetworkVariable<bool> isWalking = new NetworkVariable<bool>();
    private NetworkVariable<bool> isStunned = new NetworkVariable<bool>();


    private void Awake()
    {
        playerVisual = GetComponentInChildren<PlayerVisual>();
        weapon = GetComponentInChildren<WeaponComponent>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        capsuleCollider.center = new Vector3(0f, 1f, 0f);
        capsuleCollider.radius = 0.7f;
        capsuleCollider.height = 2.5f;

        if (IsClient && IsOwner) UpdateClientIsWalkingServerRpc(false);
    }

    private void OnDisable()
    {
        GameInput.Instance.OnInteractAction -= GameInput_OnInteractAction;
    }

    void Start()
    {
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
        MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += PlayersManager_OnPlayerDataNetworkListChanged;

        UpdatePlayerData();
    }
    private void PlayersManager_OnPlayerDataNetworkListChanged(object sender, System.EventArgs e)
    {
        UpdatePlayerData();
    }

    private void UpdatePlayerData()
    {
        PlayerData playerData = MultiplayerManager.Instance.GetPlayerDataFromClientId(OwnerClientId);
        Debug.Log("Player Color index : " + playerData.colorId);
        playerVisual.SetPlayerColor(MultiplayerManager.Instance.GetPlayerColor(playerData.colorId));
    }

    private void GameInput_OnInteractAction(object sender, System.EventArgs e)
    {
        if (IsClient && IsOwner && GameManager.Instance.IsGameState(GameState.GamePlaying))
        {
            Debug.Log("GameInput_OnInteractAction");
            weapon.HandleAttack(true, transform.forward.normalized);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleMovement();
        CheckTileCapture();
    }

    private void HandleMovement()
    {
        if (!GameManager.Instance.IsGameState(GameState.GamePlaying)) return;
        if (isStunned.Value) return;

        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();

        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        bool isWalkAnim = moveDir != Vector3.zero;
        if(IsClient && IsOwner) UpdateClientIsWalkingServerRpc(isWalkAnim);

        float moveDistance = moveSpeed * Time.deltaTime;
        transform.position += moveDir * moveDistance;
        transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
        Debug.DrawRay(transform.position, transform.forward, Color.green, 2f, false);
    }

    private void CheckTileCapture()
    {
        if(IsOwner && IsClient && GameManager.Instance.IsGameState(GameState.GamePlaying))
        {
            Vector3 rayStartPosition = new Vector3(transform.position.x, 1f, transform.position.z);
            Ray ray = new Ray(rayStartPosition, Vector3.down); 
            Debug.DrawRay(rayStartPosition, Vector3.down * raycastDistance, Color.red, 1f);

            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile != null)
                {
                    //Debug.Log($"Tile {tile} trying to be captured by player {OwnerClientId}");
                    //tile.CaptureTile(OwnerClientId, GetPlayerColor());

                    RequestTileCaptureServerRpc(tile.GetComponent<NetworkObject>().NetworkObjectId);
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestTileCaptureServerRpc(ulong tileNetworkObjectId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(tileNetworkObjectId, out var networkObject))
        {
            Debug.LogError($"Tile with NetworkObjectId {tileNetworkObjectId} not found.");
            return;
        }

        Tile tile = networkObject.GetComponent<Tile>();
        if (tile != null)
        {
            tile.CaptureTile(OwnerClientId, GetPlayerColor());
        }
    }

    public void Stun()
    {
        if (!isStunned.Value)
        {
            UpdateClientIsStunnedServerRpc(true);
            StartCoroutine(StunCoroutine());
        }
    }

    public bool IsStunned()
    { 
        return isStunned.Value;
    }

    [Rpc(SendTo.Server)]
    public void UpdateClientIsStunnedServerRpc(bool isTrue)
    {
        isStunned.Value = isTrue;
    }

    private IEnumerator StunCoroutine()
    {
        yield return new WaitForSeconds(1f);
        UpdateClientIsStunnedServerRpc(false);
    }

    public bool IsWalking()
    {
        return isWalking.Value;
    }

    [Rpc(SendTo.Server)]
    public void UpdateClientIsWalkingServerRpc(bool isTrue)
    {
        isWalking.Value = isTrue;
    }

    public Color GetPlayerColor()
    {
        return playerVisual.GetPlayerColor();
    }
}
