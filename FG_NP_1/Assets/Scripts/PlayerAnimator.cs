using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class PlayerAnimator : NetworkBehaviour
{
    private const string IS_WALKING = "IsWalking";

    [SerializeField] private GamePlayer player;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        UpdateClientAnimationServerRpc();
    }

    [ServerRpc]
    public void UpdateClientAnimationServerRpc()
    {
        animator.SetBool(IS_WALKING, player.IsWalking());
    }
}
