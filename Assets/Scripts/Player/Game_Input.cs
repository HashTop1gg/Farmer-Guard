using UnityEngine;

public class Game_Input : MonoBehaviour
{
    private PlayerInputAction PlayerInputAction;
    public static Game_Input Instance {get; set;}

    private void Awake()
    {
        Instance = this;
        PlayerInputAction = new PlayerInputAction();
        PlayerInputAction.Enable();
    }
    
    public Vector2 GetMovementVector()
    {
        Vector2 moveVector = PlayerInputAction.Player.Move.ReadValue<Vector2>();
        return moveVector;
    }
}
