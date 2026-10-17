using UnityEngine;

public class Player_Script : MonoBehaviour
{
    [SerializeField] float movingSpeed = 5f;
    private Rigidbody2D rb;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
   private void FixedUpdate(){
    Vector2 moveVector = Game_Input.Instance.GetMovementVector();
        moveVector = moveVector.normalized;
        Debug.Log(moveVector);
        rb.MovePosition(rb.position + moveVector * (movingSpeed * Time.fixedDeltaTime));
    }
}
