using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player_Script : MonoBehaviour, IDamageable
{
    public event Action<int, int> OnHealthChanged;
    public event Action OnPlayerDied;

    [Header("Movement")]
    [SerializeField] private float movingSpeed = 5f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [Header("Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private int currentHealth;
    private bool isDead;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int LastXHash = Animator.StringToHash("LastX");
    private static readonly int LastYHash = Animator.StringToHash("LastY");

    public Vector2 FacingDirection { get; private set; } = Vector2.right;

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;

        if (rb.bodyType != RigidbodyType2D.Dynamic)
        {
            Debug.LogWarning("Player_Script: Rigidbody2D переведено в Dynamic, щоб працювали колізії.", this);
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        if (isDead || Game_Input.Instance == null || rb == null) return;

        Vector2 moveVector = Game_Input.Instance.GetMovementVector().normalized;

        if (moveVector.sqrMagnitude > 0.0001f)
            FacingDirection = moveVector;

        if (spriteRenderer != null)
        {
            if (moveVector.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (moveVector.x < -0.01f)
                spriteRenderer.flipX = true;
        }

        UpdateAnimator(moveVector);

        rb.MovePosition(rb.position + moveVector * (movingSpeed * Time.fixedDeltaTime));
    }

    private void UpdateAnimator(Vector2 moveVector)
    {
        if (animator == null) return;

        Vector2 animDir = moveVector;

        if (Mathf.Abs(animDir.x) > Mathf.Abs(animDir.y))
            animDir.y = 0f;
        else
            animDir.x = 0f;

        animator.SetFloat(MoveXHash, animDir.x);
        animator.SetFloat(MoveYHash, animDir.y);

        if (animDir.sqrMagnitude > 0.01f)
        {
            animator.SetFloat(LastXHash, animDir.x);
            animator.SetFloat(LastYHash, animDir.y);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            isDead = true;
            OnPlayerDied?.Invoke();
            Debug.Log("Player died");
        }
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}