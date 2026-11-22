using UnityEngine;
using UnityEngine.InputSystem;

public class Game_Input : MonoBehaviour
{
    private InputSystem_Actions playerInputAction;
    public static Game_Input Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        playerInputAction = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        playerInputAction?.Enable();
    }

    private void OnDisable()
    {
        playerInputAction?.Disable();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public Vector2 GetMovementVector()
    {
        if (playerInputAction == null) return Vector2.zero;
        return playerInputAction.Player.Move.ReadValue<Vector2>();
    }

    public bool WasAttackPressedThisFrame()
    {
        if (playerInputAction == null) return false;
        return playerInputAction.Player.Attack.WasPressedThisFrame();
    }

    public bool WasInteractPressedThisFrame()
    {
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    }
}