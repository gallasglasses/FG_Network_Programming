using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    public event EventHandler OnInteractAction;

    private enum BindingState
    {
        UP,
        DOWN,
        LEFT,
        RIGHT,
        INTERACT
    }

    private PlayerInputActions _playerInputActions;

    private void Awake()
    {
        Instance = this;

        _playerInputActions = new PlayerInputActions();
        _playerInputActions.Player.Enable();
    }
    private void OnEnable()
    {
        _playerInputActions.Player.Interact.performed += Interact_performed;
    }

    private void OnDisable()
    {
        _playerInputActions.Player.Interact.performed -= Interact_performed;

        _playerInputActions.Player.Disable();
    }

    private void Interact_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnInteractAction?.Invoke(this, EventArgs.Empty);
    }

    public Vector2 GetMovementVectorNormalized()
    {
        Vector2 inputVector = _playerInputActions.Player.Move.ReadValue<Vector2>();
        return inputVector.normalized;
    }
}
