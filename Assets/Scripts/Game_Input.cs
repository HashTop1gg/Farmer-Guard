using UnityEngine;

public class Game_Input : MonoBehaviour
{
    public static Game_Input Instance {get; private set;}
    private Player_input_Action Player_input_Action;
    private void Awake()
    {
        Instance = this;
        Player_input_Action = new Player_input_Action();
        Player_input_Action.Enable();
    }
     public Vector2 GetMovementVector()
    {
        Vector2 moveVector = Player_input_Action.Player.Move.ReadValue<Vector2>();
        return moveVector;
    }
}
