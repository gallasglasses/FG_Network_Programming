using System;
using Unity.Netcode;
using UnityEngine;
//using UnityEngine.InputSystem;

[RequireComponent(typeof(CapsuleCollider))]
//[RequireComponent(typeof(PlayerInput))]
public class Player : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float rotateSpeed = 10f;

    private WeaponComponent weapon;
    private CapsuleCollider capsuleCollider;
    private NetworkVariable<bool> isWalking = new NetworkVariable<bool>();

    //private PlayerInput _playerInput;
    //private InputAction _move;
    //private InputAction _interact;

    private void Awake()
    {
        weapon = GetComponentInChildren<WeaponComponent>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        capsuleCollider.center = new Vector3(0f, 1f, 0f);
        capsuleCollider.radius = 0.7f;
        capsuleCollider.height = 2f;

        if (IsClient && IsOwner) UpdateClientIsWalkingServerRpc(false);
        //_playerInput = GetComponent<PlayerInput>();
        //if (_playerInput != null )
        //{
        //    Debug.Log("_playerInput != null");
        //    _move = _playerInput.actions["Move"];
        //    _interact = _playerInput.actions["Interact"];
        //}
    }

    //private void OnEnable()
    //{
    //    _move.Enable();
    //    _interact.Enable();
    //    _interact.performed += Interact_performed;
    //}

    private void OnDisable()
    {
        GameInput.Instance.OnInteractAction -= GameInput_OnInteractAction;
        //_move.Disable();

        //_interact.performed -= Interact_performed;
        //_interact.Disable();
    }

    void Start()
    {
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    //private void Interact_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    //{
    //    Debug.Log("GameInput_OnInteractAction");
    //}

    private void GameInput_OnInteractAction(object sender, System.EventArgs e)
    {
        if (IsClient && IsOwner)
        {
            Debug.Log("GameInput_OnInteractAction");
            weapon.HandleAttack(true, transform.forward.normalized);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        //Debug.Log("HandleMovement");

        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();
        //Vector2 inputVector = GetMovementVectorNormalized();

        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        bool isWalkAnim = moveDir != Vector3.zero;
        if(IsClient && IsOwner) UpdateClientIsWalkingServerRpc(isWalkAnim);

        float moveDistance = moveSpeed * Time.deltaTime;
        transform.position += moveDir * moveDistance;
        transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
        Debug.DrawRay(transform.position, transform.forward, Color.green, 2f, false);
    }

    public bool IsWalking()
    {
        return isWalking.Value;
    }

    [ServerRpc]
    public void UpdateClientIsWalkingServerRpc(bool isTrue)
    {
        isWalking.Value = isTrue;
    }

    //public Vector2 GetMovementVectorNormalized()
    //{
    //    Vector2 inputVector = _move.ReadValue<Vector2>();
    //    return inputVector.normalized;
    //}
}
