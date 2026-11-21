using UnityEngine;

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
        return playerInputAction.Player.Move.ReadValue<Vector2>();
    }
    public bool WasAttackPressedThisFrame()
    {
        return playerInputAction.Player.Attack.WasPressedThisFrame();
    }
}